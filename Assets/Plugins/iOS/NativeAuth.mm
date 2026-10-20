// Native iOS social sign-in bridge for Unity. Unity compiles this file from
// Assets/Plugins/iOS into the Xcode project automatically; Unity calls these
// functions via DllImport("__Internal"). Results are delivered back to the
// "NativeAuthDispatcher" GameObject via UnitySendMessage as JSON.
//
// Google: plain OAuth 2.0 authorization-code flow with PKCE, hosted in
// ASWebAuthenticationSession — no Google SDK, no CocoaPods. The redirect URI
// is Google's installed-app reverse client ID scheme
// (com.googleusercontent.apps.<CLIENT_ID>:/oauthredirect), so the returned ID
// token audience is the iOS OAuth client ID; that client ID must be listed in
// the backend's Authentication__Google__ClientIds.
//
// Apple: native Sign in with Apple (ASAuthorizationAppleIDProvider). The
// identity token audience is the app's bundle ID, which the backend expects
// in Authentication__Apple__ClientIds. Requires the Sign in with Apple
// entitlement (added by Assets/Editor/iOS/NativeAuthPostProcess.cs).

#import <Foundation/Foundation.h>
#import <UIKit/UIKit.h>
#import <AuthenticationServices/AuthenticationServices.h>
#import <CommonCrypto/CommonDigest.h>
#import <Security/Security.h>

extern "C" void UnitySendMessage(const char *gameObject, const char *methodName, const char *message);

static NSString *const kCallbackObject = @"NativeAuthDispatcher";
static NSString *const kCallbackResult = @"OnNativeAuthResult";
static NSString *const kCallbackEvent = @"OnNativeAuthEvent";

#pragma mark - Result delivery

static void LovieAuthSendResult(NSDictionary *payload)
{
    NSError *error = nil;
    NSData *data = [NSJSONSerialization dataWithJSONObject:payload options:0 error:&error];
    if (data == nil)
    {
        NSMutableDictionary *fallback = [payload mutableCopy];
        fallback[@"errorMessage"] = @"Failed to serialize native sign-in result.";
        data = [NSJSONSerialization dataWithJSONObject:fallback options:0 error:nil];
    }
    NSString *json = [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding];
    UnitySendMessage(kCallbackObject.UTF8String, kCallbackResult.UTF8String, json.UTF8String ?: "{}");
}

static NSDictionary *LovieAuthFailure(NSString *provider, NSString *code, NSString *message)
{
    return @{
        @"success": @NO,
        @"provider": provider,
        @"error": code ?: @"unknown",
        @"errorMessage": message ?: @"",
    };
}

#pragma mark - Helpers

// Key window across scene-based (iOS 15+) and legacy UIApplication lifecycles.
static UIWindow *LovieAuthKeyWindow(void)
{
    for (UIScene *scene in UIApplication.sharedApplication.connectedScenes)
    {
        if (![scene isKindOfClass:[UIWindowScene class]]) continue;
        if (scene.activationState != UISceneActivationStateForegroundActive) continue;
        for (UIWindow *window in ((UIWindowScene *)scene).windows)
        {
            if (window.isKeyWindow) return window;
        }
    }

    // Pre-scene fallback (Unity's classic single-window lifecycle).
    id<UIApplicationDelegate> appDelegate = UIApplication.sharedApplication.delegate;
    if ([appDelegate respondsToSelector:@selector(window)])
    {
        UIWindow *window = [appDelegate performSelector:@selector(window)];
        if ([window isKindOfClass:[UIWindow class]]) return window;
    }
    return nil;
}

static NSString *LovieAuthRandomBase64Url(NSUInteger byteCount)
{
    uint8_t bytes[64];
    if (byteCount > sizeof(bytes)) byteCount = sizeof(bytes);
    if (SecRandomCopyBytes(kSecRandomDefault, byteCount, bytes) != errSecSuccess) return nil;

    NSMutableString *hex = [NSMutableString stringWithCapacity:byteCount * 2];
    for (NSUInteger i = 0; i < byteCount; i++) [hex appendFormat:@"%02x", bytes[i]];
    return hex.copy; // hex is URL-safe and valid for code_verifier/state/nonce
}

static NSString *LovieAuthSha256Base64Url(NSString *input)
{
    NSData *data = [input dataUsingEncoding:NSUTF8StringEncoding];
    uint8_t digest[CC_SHA256_DIGEST_LENGTH];
    CC_SHA256(data.bytes, (CC_LONG)data.length, digest);
    NSData *digestData = [NSData dataWithBytes:digest length:sizeof(digest)];

    NSString *base64 = [digestData base64EncodedStringWithOptions:0];
    return [[base64 stringByReplacingOccurrencesOfString:@"+" withString:@"-"]
        stringByReplacingOccurrencesOfString:@"/" withString:@"_"];
}

// Best-effort JWT payload decode so the C# side gets the profile without
// another network round-trip. Signatures are verified by the backend.
static NSDictionary *LovieAuthJwtPayload(NSString *jwt)
{
    if (jwt.length == 0) return nil;

    NSArray *parts = [jwt componentsSeparatedByString:@"."];
    if (parts.count < 2) return nil;

    NSString *payload = parts[1];
    NSMutableString *base64 = [payload mutableCopy];
    [base64 replaceOccurrencesOfString:@"-" withString:@"+" options:0 range:NSMakeRange(0, base64.length)];
    [base64 replaceOccurrencesOfString:@"_" withString:@"/" options:0 range:NSMakeRange(0, base64.length)];
    while (base64.length % 4 != 0) [base64 appendString:@"="];

    NSData *data = [[NSData alloc] initWithBase64EncodedString:base64 options:0];
    if (data == nil) return nil;

    id decoded = [NSJSONSerialization JSONObjectWithData:data options:0 error:nil];
    return [decoded isKindOfClass:[NSDictionary class]] ? decoded : nil;
}

#pragma mark - Google OAuth (PKCE, ASWebAuthenticationSession)

@interface LovieGoogleAuth : NSObject <ASWebAuthenticationPresentationContextProviding>
@property (nonatomic, strong) ASWebAuthenticationSession *session;
@property (nonatomic, copy) NSString *codeVerifier;
@property (nonatomic, copy) NSString *state;
@end

@implementation LovieGoogleAuth

+ (instancetype)shared
{
    static LovieGoogleAuth *instance;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{ instance = [[LovieGoogleAuth alloc] init]; });
    return instance;
}

- (NSString *)redirectSchemeForClientId:(NSString *)clientId
{
    return [NSString stringWithFormat:@"com.googleusercontent.apps.%@", clientId];
}

- (void)signInWithClientId:(NSString *)clientId
{
    if (clientId.length == 0)
    {
        LovieAuthSendResult(LovieAuthFailure(@"Google", @"config_missing",
            @"Google iOS client ID is not configured. Set it on the ApiConfig asset (Google iOS Client Id)."));
        return;
    }

    if (self.session != nil)
    {
        [self.session cancel];
        self.session = nil;
    }

    NSString *codeVerifier = LovieAuthRandomBase64Url(32); // 64 hex chars, within PKCE's 43..128
    NSString *state = LovieAuthRandomBase64Url(16);
    if (codeVerifier == nil || state == nil)
    {
        LovieAuthSendResult(LovieAuthFailure(@"Google", @"security_error", @"Failed to generate PKCE parameters."));
        return;
    }
    self.codeVerifier = codeVerifier;
    self.state = state;

    NSString *scheme = [self redirectSchemeForClientId:clientId];
    NSString *redirectUri = [scheme stringByAppendingString:@":/oauthredirect"];

    NSURLComponents *components = [NSURLComponents componentsWithString:@"https://accounts.google.com/o/oauth2/v2/auth"];
    components.queryItems = @[
        [NSURLQueryItem queryItemWithName:@"response_type" value:@"code"],
        [NSURLQueryItem queryItemWithName:@"client_id" value:clientId],
        [NSURLQueryItem queryItemWithName:@"redirect_uri" value:redirectUri],
        [NSURLQueryItem queryItemWithName:@"scope" value:@"openid email profile"],
        [NSURLQueryItem queryItemWithName:@"state" value:state],
        [NSURLQueryItem queryItemWithName:@"nonce" value:LovieAuthRandomBase64Url(16)],
        [NSURLQueryItem queryItemWithName:@"code_challenge" value:LovieAuthSha256Base64Url(codeVerifier)],
        [NSURLQueryItem queryItemWithName:@"code_challenge_method" value:@"S256"],
    ];

    ASWebAuthenticationSession *session = [[ASWebAuthenticationSession alloc]
        initWithURL:components.URL
        callbackURLScheme:scheme
        completionHandler:^(NSURL *callbackURL, NSError *error)
        {
            self.session = nil;

            if (error != nil)
            {
                BOOL cancelled = [error.domain isEqualToString:ASWebAuthenticationSessionErrorDomain]
                    && error.code == ASWebAuthenticationSessionErrorCodeCanceledLogin;
                LovieAuthSendResult(LovieAuthFailure(@"Google", cancelled ? @"cancelled" : @"web_session_failed",
                    error.localizedDescription));
                return;
            }

            NSString *code = nil;
            NSString *returnedState = nil;
            for (NSURLQueryItem *item in [NSURLComponents componentsWithURL:callbackURL resolvingAgainstBaseURL:NO].queryItems)
            {
                if ([item.name isEqualToString:@"code"]) code = item.value;
                if ([item.name isEqualToString:@"state"]) returnedState = item.value;
            }

            if (code.length == 0)
            {
                LovieAuthSendResult(LovieAuthFailure(@"Google", @"authorization_denied",
                    @"Google did not return an authorization code."));
                return;
            }
            if (returnedState.length == 0 || ![returnedState isEqualToString:self.state])
            {
                LovieAuthSendResult(LovieAuthFailure(@"Google", @"state_mismatch",
                    @"OAuth state mismatch — possible replay or tampering."));
                return;
            }

            [self exchangeCode:code clientId:clientId redirectUri:redirectUri];
        }];

    session.presentationContextProvider = self;
    // Keep the shared cookie session so returning users don't re-enter passwords.
    session.prefersEphemeralWebBrowserSession = NO;
    self.session = session;
    [session start];
}

// No client secret: Google's token endpoint accepts PKCE-verified code
// exchanges for installed-app (iOS) OAuth clients.
- (void)exchangeCode:(NSString *)code clientId:(NSString *)clientId redirectUri:(NSString *)redirectUri
{
    NSURLComponents *components = [NSURLComponents componentsWithString:@"https://oauth2.googleapis.com/token"];
    components.queryItems = @[
        [NSURLQueryItem queryItemWithName:@"grant_type" value:@"authorization_code"],
        [NSURLQueryItem queryItemWithName:@"code" value:code],
        [NSURLQueryItem queryItemWithName:@"client_id" value:clientId],
        [NSURLQueryItem queryItemWithName:@"code_verifier" value:self.codeVerifier],
        [NSURLQueryItem queryItemWithName:@"redirect_uri" value:redirectUri],
    ];

    NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:components.URL];
    request.HTTPMethod = @"POST";
    request.HTTPBody = [components.query dataUsingEncoding:NSUTF8StringEncoding];
    [request setValue:@"application/x-www-form-urlencoded" forHTTPHeaderField:@"Content-Type"];

    NSURLSessionDataTask *task = [[NSURLSession sharedSession] dataTaskWithRequest:request
        completionHandler:^(NSData *data, NSURLResponse *response, NSError *error)
        {
            if (error != nil)
            {
                LovieAuthSendResult(LovieAuthFailure(@"Google", @"network_error", error.localizedDescription));
                return;
            }

            NSHTTPURLResponse *http = (NSHTTPURLResponse *)response;
            id body = data.length > 0 ? [NSJSONSerialization JSONObjectWithData:data options:0 error:nil] : nil;
            NSDictionary *json = [body isKindOfClass:[NSDictionary class]] ? body : nil;

            if (http.statusCode < 200 || http.statusCode >= 300 || json == nil)
            {
                NSString *detail = json[@"error_description"] ?: json[@"error"] ?: @(http.statusCode).stringValue;
                LovieAuthSendResult(LovieAuthFailure(@"Google", @"token_exchange_failed",
                    [NSString stringWithFormat:@"Google token exchange failed: %@", detail]));
                return;
            }

            NSString *idToken = json[@"id_token"];
            if (idToken.length == 0)
            {
                LovieAuthSendResult(LovieAuthFailure(@"Google", @"token_exchange_failed",
                    @"Google token response did not include an ID token."));
                return;
            }

            NSDictionary *claims = LovieAuthJwtPayload(idToken);
            NSMutableDictionary *result = [@{
                @"success": @YES,
                @"provider": @"Google",
                @"idToken": idToken,
                @"userId": claims[@"sub"] ?: @"",
                @"email": claims[@"email"] ?: @"",
                @"displayName": claims[@"name"] ?: @"",
                @"photoUrl": claims[@"picture"] ?: @"",
            } mutableCopy];
            if (claims[@"given_name"] != nil) result[@"givenName"] = claims[@"given_name"];
            if (claims[@"family_name"] != nil) result[@"familyName"] = claims[@"family_name"];

            LovieAuthSendResult(result);
        }];
    [task resume];
}

- (void)cancel
{
    [self.session cancel];
    self.session = nil;
}

- (ASPresentationAnchor)presentationAnchorForWebAuthenticationSession:(ASWebAuthenticationSession *)session
{
    return LovieAuthKeyWindow() ?: [[UIWindow alloc] init];
}

@end

#pragma mark - Sign in with Apple

@interface LovieAppleAuth : NSObject <ASAuthorizationControllerDelegate, ASAuthorizationControllerPresentationContextProviding>
@end

@implementation LovieAppleAuth

+ (instancetype)shared
{
    static LovieAppleAuth *instance;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{ instance = [[LovieAppleAuth alloc] init]; });
    return instance;
}

- (void)signIn
{
    if (@available(iOS 13.0, *))
    {
        ASAuthorizationAppleIDRequest *request = [[[ASAuthorizationAppleIDProvider alloc] init] createRequest];
        // Name and email are only reported on the very first authorization.
        request.requestedScopes = @[ASAuthorizationScopeFullName, ASAuthorizationScopeEmail];

        ASAuthorizationController *controller = [[ASAuthorizationController alloc] initWithAuthorizationRequests:@[request]];
        controller.delegate = self;
        controller.presentationContextProvider = self;
        [controller performRequests];
    }
    else
    {
        LovieAuthSendResult(LovieAuthFailure(@"Apple", @"unsupported", @"Sign in with Apple requires iOS 13 or later."));
    }
}

- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithAuthorization:(ASAuthorization *)authorization
{
    ASAuthorizationAppleIDCredential *credential = authorization.credential;
    if (![credential isKindOfClass:[ASAuthorizationAppleIDCredential class]])
    {
        LovieAuthSendResult(LovieAuthFailure(@"Apple", @"unexpected_credential",
            @"Apple returned an unexpected credential type."));
        return;
    }

    NSString *idToken = [[NSString alloc] initWithData:credential.identityToken encoding:NSUTF8StringEncoding];
    if (idToken.length == 0)
    {
        LovieAuthSendResult(LovieAuthFailure(@"Apple", @"token_missing", @"Apple sign-in returned no identity token."));
        return;
    }

    NSString *givenName = credential.fullName.givenName ?: @"";
    NSString *familyName = credential.fullName.familyName ?: @"";
    NSString *displayName = [@[givenName, familyName] componentsJoinedByString:@" "];
    displayName = [displayName stringByTrimmingCharactersInSet:[NSCharacterSet whitespaceCharacterSet]];

    LovieAuthSendResult(@{
        @"success": @YES,
        @"provider": @"Apple",
        @"idToken": idToken,
        // Backend expects firstName/lastName only on the first sign-in, when
        // Apple still reveals them.
        @"givenName": givenName,
        @"familyName": familyName,
        @"userId": credential.user ?: @"",
        @"email": credential.email ?: @"",
        @"displayName": displayName,
    });
}

- (void)authorizationController:(ASAuthorizationController *)controller didCompleteWithError:(NSError *)error
{
    NSString *code = @"apple_error";
    if (error.code == ASAuthorizationErrorCanceled) code = @"cancelled";
    else if (error.code == ASAuthorizationErrorNotHandled) code = @"not_handled";
    else if (error.code == ASAuthorizationErrorFailed) code = @"failed";
    LovieAuthSendResult(LovieAuthFailure(@"Apple", code, error.localizedDescription));
}

- (ASPresentationAnchor)presentationAnchorForAuthorizationController:(ASAuthorizationController *)controller
{
    return LovieAuthKeyWindow() ?: [[UIWindow alloc] init];
}

@end

#pragma mark - Unity entry points

extern "C"
{

void LovieAuth_GoogleSignIn(const char *clientId)
{
    NSString *id = clientId != NULL ? [NSString stringWithUTF8String:clientId] : nil;
    dispatch_async(dispatch_get_main_queue(), ^{
        [[LovieGoogleAuth shared] signInWithClientId:id ?: @""];
    });
}

void LovieAuth_GoogleCancel(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        [[LovieGoogleAuth shared] cancel];
    });
}

void LovieAuth_AppleSignIn(void)
{
    dispatch_async(dispatch_get_main_queue(), ^{
        [[LovieAppleAuth shared] signIn];
    });
}

// Reports {"type":"apple_credential_state","userId":...,"value":"authorized"|"revoked"|"notFound"|"transferred"|"unknown"}
void LovieAuth_AppleGetCredentialState(const char *userId)
{
    if (userId == NULL) return;
    NSString *appleUserId = [NSString stringWithUTF8String:userId];
    if (appleUserId.length == 0) return;

    if (@available(iOS 13.0, *))
    {
        ASAuthorizationAppleIDProvider *provider = [[ASAuthorizationAppleIDProvider alloc] init];
        [provider getCredentialStateForUserID:appleUserId completion:^(ASAuthorizationAppleIDProviderCredentialState credentialState, NSError *error)
        {
            NSString *value = @"unknown";
            switch (credentialState)
            {
                case ASAuthorizationAppleIDProviderCredentialAuthorized: value = @"authorized"; break;
                case ASAuthorizationAppleIDProviderCredentialRevoked: value = @"revoked"; break;
                case ASAuthorizationAppleIDProviderCredentialNotFound: value = @"notFound"; break;
                case ASAuthorizationAppleIDProviderCredentialTransferred: value = @"transferred"; break;
            }

            NSData *data = [NSJSONSerialization dataWithJSONObject:@{
                @"type": @"apple_credential_state",
                @"userId": appleUserId,
                @"value": value,
            } options:0 error:nil];
            NSString *json = data != nil ? [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] : @"{}";
            UnitySendMessage(kCallbackObject.UTF8String, kCallbackEvent.UTF8String, json.UTF8String ?: "{}");
        }];
    }
}

} // extern "C"

package com.lovie.auth;

import android.app.Activity;

import com.unity3d.player.UnityPlayer;

import org.json.JSONObject;

/**
 * Entry point for native social sign-in on Android. Unity calls these static
 * methods through AndroidJavaClass; results are delivered back to the
 * "NativeAuthDispatcher" GameObject on the C# side via UnitySendMessage as a
 * JSON payload (parsed into Api.NativeAuthResult).
 *
 * Google sign-in uses the Credential Manager API through a transparent
 * trampoline activity (GoogleCredentialActivity), because Credential Manager
 * requires a ComponentActivity and Unity's player activity is not one.
 */
public final class AuthBridge
{
    static final String CALLBACK_GAME_OBJECT = "NativeAuthDispatcher";
    static final String CALLBACK_METHOD = "OnNativeAuthResult";

    private AuthBridge()
    {
    }

    /**
     * Starts the Google Credential Manager flow for the given web "server"
     * client ID. The resulting ID token has this client ID as its audience,
     * matching what the backend verifies.
     */
    public static void signInWithGoogle(Activity activity, String serverClientId)
    {
        GoogleCredentialActivity.resetForNewSignIn();
        GoogleCredentialActivity.start(activity, serverClientId);
    }

    /** Dismisses a Google Credential Manager flow that is showing its UI. */
    public static void cancelGoogleSignIn()
    {
        GoogleCredentialActivity.cancelExisting();
    }

    static void sendGoogleSuccess(String idToken, String userId, String displayName,
                                  String givenName, String familyName, String photoUrl)
    {
        JSONObject json = new JSONObject();
        try
        {
            json.put("success", true);
            json.put("provider", "Google");
            json.put("idToken", idToken);
            json.put("userId", userId);
            json.put("displayName", displayName);
            json.put("givenName", givenName);
            json.put("familyName", familyName);
            json.put("photoUrl", photoUrl);
        }
        catch (Exception ignored)
        {
            // Values are all nullable strings; JSONObject.put never throws here.
        }
        sendJson(json);
    }

    static void sendGoogleFailure(String errorCode, String errorMessage)
    {
        JSONObject json = new JSONObject();
        try
        {
            json.put("success", false);
            json.put("provider", "Google");
            json.put("error", errorCode);
            json.put("errorMessage", errorMessage);
        }
        catch (Exception ignored)
        {
        }
        sendJson(json);
    }

    private static void sendJson(JSONObject json)
    {
        try
        {
            UnityPlayer.UnitySendMessage(CALLBACK_GAME_OBJECT, CALLBACK_METHOD, json.toString());
        }
        catch (Exception e)
        {
            android.util.Log.e("LovieAuth", "Failed to deliver result to Unity", e);
        }
    }
}

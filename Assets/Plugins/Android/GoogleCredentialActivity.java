package com.lovie.auth;

import android.app.Activity;
import android.content.Intent;
import android.os.CancellationSignal;
import android.os.Bundle;

import androidx.activity.ComponentActivity;
import androidx.credentials.Credential;
import androidx.credentials.CredentialManager;
import androidx.credentials.CredentialManagerCallback;
import androidx.credentials.CustomCredential;
import androidx.credentials.GetCredentialRequest;
import androidx.credentials.GetCredentialResponse;
import androidx.credentials.exceptions.GetCredentialCancellationException;
import androidx.credentials.exceptions.GetCredentialException;
import androidx.credentials.exceptions.GetCredentialInterruptedException;
import androidx.credentials.exceptions.NoCredentialException;

import com.google.android.libraries.identity.googleid.GetGoogleIdOption;
import com.google.android.libraries.identity.googleid.GoogleIdTokenCredential;

import java.util.concurrent.Executor;
import java.util.concurrent.Executors;

/**
 * Transparent trampoline activity hosting the Google Credential Manager flow.
 * Credential Manager requires a ComponentActivity; Unity's player activity is
 * a plain Activity, so this activity launches, runs the flow over the Unity
 * content, reports the ID token to AuthBridge and finishes itself.
 *
 * First run filters to accounts already authorized with the app; when none
 * exist (NoCredentialException) it relaunches once with the full account
 * picker so brand-new users can sign in.
 */
public final class GoogleCredentialActivity extends ComponentActivity
{
    /** Filters flow to accounts the user already authorized for this app. */
    private static boolean filterByAuthorizedAccounts = true;

    /** Running instance, used to cancel the flow. */
    private static GoogleCredentialActivity current;

    private boolean launched;

    static void start(Activity activity, String serverClientId)
    {
        Intent intent = new Intent(activity, GoogleCredentialActivity.class);
        intent.putExtra("serverClientId", serverClientId);
        intent.putExtra("filterAuthorized", filterByAuthorizedAccounts);
        activity.startActivity(intent);
    }

    static void resetForNewSignIn()
    {
        filterByAuthorizedAccounts = true;
    }

    /** Dismisses a running flow; finishing the host activity cancels it. */
    static void cancelExisting()
    {
        GoogleCredentialActivity instance = current;
        if (instance != null) instance.finish();
    }

    @Override
    protected void onCreate(Bundle savedInstanceState)
    {
        super.onCreate(savedInstanceState);
        current = this;
        // Relaunching for the account-picker retry restores the flag the
        // intent was launched with, keeping state across activity instances.
        filterByAuthorizedAccounts = getIntent().getBooleanExtra("filterAuthorized", filterByAuthorizedAccounts);
    }

    @Override
    protected void onDestroy()
    {
        if (current == this) current = null;
        super.onDestroy();
    }

    @Override
    protected void onResume()
    {
        super.onResume();
        if (launched) return;
        launched = true;
        runFlow();
    }

    private void runFlow()
    {
        final String serverClientId = getIntent().getStringExtra("serverClientId");
        if (serverClientId == null || serverClientId.trim().isEmpty())
        {
            finishWithFailure("config_missing",
                    "Google server client ID is not configured. Set it on the ApiConfig asset (Google Server Client Id).");
            return;
        }

        GetGoogleIdOption googleIdOption = new GetGoogleIdOption.Builder()
                .setFilterByAuthorizedAccounts(filterByAuthorizedAccounts)
                .setServerClientId(serverClientId)
                .setAutoSelectEnabled(false)
                .build();

        GetCredentialRequest request = new GetCredentialRequest.Builder()
                .addCredentialOption(googleIdOption)
                .build();

        CredentialManager credentialManager = CredentialManager.create(this);
        Executor executor = Executors.newSingleThreadExecutor();
        CancellationSignal cancellationSignal = new CancellationSignal();

        credentialManager.getCredentialAsync(this, request, cancellationSignal, executor,
                new CredentialManagerCallback<GetCredentialResponse, GetCredentialException>()
                {
                    @Override
                    public void onResult(GetCredentialResponse response)
                    {
                        handleResult(response);
                    }

                    @Override
                    public void onError(GetCredentialException e)
                    {
                        handleError(e);
                    }
                });
    }

    private void handleResult(GetCredentialResponse response)
    {
        Credential credential = response.getCredential();
        if (credential instanceof CustomCredential
                && GoogleIdTokenCredential.TYPE_GOOGLE_ID_TOKEN_CREDENTIAL.equals(credential.getType()))
        {
            GoogleIdTokenCredential googleCredential = GoogleIdTokenCredential.createFrom(credential.getData());
            filterByAuthorizedAccounts = true;
            final String idToken = googleCredential.getIdToken();
            runOnUiThread(() -> {
                AuthBridge.sendGoogleSuccess(idToken,
                        googleCredential.getId(),
                        googleCredential.getDisplayName(),
                        googleCredential.getGivenName(),
                        googleCredential.getFamilyName(),
                        googleCredential.getProfilePictureUri() == null
                                ? null
                                : googleCredential.getProfilePictureUri().toString());
                finish();
            });
        }
        else
        {
            filterByAuthorizedAccounts = true;
            finishWithFailure("unexpected_credential",
                    "Credential Manager returned an unexpected credential type: " + credential.getType());
        }
    }

    private void handleError(GetCredentialException e)
    {
        if (e instanceof NoCredentialException && filterByAuthorizedAccounts)
        {
            // No account on this device was previously authorized with the app:
            // relaunch once with the full account picker visible.
            filterByAuthorizedAccounts = false;
            final GoogleCredentialActivity self = this;
            final String serverClientId = getIntent().getStringExtra("serverClientId");
            runOnUiThread(() -> {
                Intent intent = new Intent(self, GoogleCredentialActivity.class);
                intent.putExtra("serverClientId", serverClientId);
                intent.putExtra("filterAuthorized", false);
                startActivity(intent);
                finish();
            });
            return;
        }

        filterByAuthorizedAccounts = true;
        String code = "google_error";
        if (e instanceof GetCredentialCancellationException) code = "cancelled";
        else if (e instanceof GetCredentialInterruptedException) code = "interrupted";
        else if (e instanceof NoCredentialException) code = "no_credentials";

        finishWithFailure(code, e.getMessage());
    }

    private void finishWithFailure(String errorCode, String errorMessage)
    {
        runOnUiThread(() -> {
            AuthBridge.sendGoogleFailure(errorCode, errorMessage);
            finish();
        });
    }
}

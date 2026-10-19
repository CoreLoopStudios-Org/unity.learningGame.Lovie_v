package com.lovie.tts;

import android.app.Activity;
import android.os.Bundle;
import android.speech.tts.TextToSpeech;
import java.util.Locale;

/**
 * Native Android TTS bridge for Unity. Unity obtains the singleton via
 * getInstance(Activity) and calls the methods through AndroidJavaObject.
 * onInit fires on a binder thread, hence the volatile fields.
 */
public class TTSBridge implements TextToSpeech.OnInitListener
{
    private static final String UTTERANCE_ID = "lovie_tts";
    private static volatile TTSBridge instance;

    private final TextToSpeech tts;
    private volatile boolean ready = false;
    private volatile String pendingLanguage;
    private volatile float speechRate = 1.0f;

    public static TTSBridge getInstance(Activity activity)
    {
        if (instance == null)
        {
            synchronized (TTSBridge.class)
            {
                if (instance == null)
                {
                    instance = new TTSBridge(activity);
                }
            }
        }
        return instance;
    }

    private TTSBridge(Activity activity)
    {
        tts = new TextToSpeech(activity.getApplicationContext(), this);
    }

    @Override
    public void onInit(int status)
    {
        ready = status == TextToSpeech.SUCCESS;
        if (!ready) return;

        if (pendingLanguage != null) applyLanguage(pendingLanguage);
        tts.setSpeechRate(speechRate);
    }

    public boolean isReady()
    {
        return ready;
    }

    /** Returns a TextToSpeech.LANG_* code; queues until init when called early. */
    public int setLanguage(String tag)
    {
        pendingLanguage = tag;
        if (!ready) return TextToSpeech.LANG_MISSING_DATA;
        return applyLanguage(tag);
    }

    private int applyLanguage(String tag)
    {
        Locale locale = null;
        if (tag != null && !tag.isEmpty())
        {
            try
            {
                locale = Locale.forLanguageTag(tag);
            }
            catch (Exception ignored)
            {
            }
        }
        if (locale == null) locale = Locale.getDefault();

        int result = tts.setLanguage(locale);
        if (result == TextToSpeech.LANG_MISSING_DATA || result == TextToSpeech.LANG_NOT_SUPPORTED)
        {
            // Requested language not installed — fall back to the device locale.
            tts.setLanguage(Locale.getDefault());
        }
        return result;
    }

    /** Unity-friendly rate: 1.0 = normal, 0.5 = half speed, 2.0 = double. */
    public void setSpeechRate(float rate)
    {
        speechRate = clampRate(rate);
        if (ready) tts.setSpeechRate(speechRate);
    }

    public boolean isSpeaking()
    {
        return ready && tts.isSpeaking();
    }

    /** Interrupts any current utterance (QUEUE_FLUSH) and speaks the new text. */
    public void speak(String text)
    {
        if (!ready || text == null || text.trim().isEmpty()) return;
        tts.speak(text, TextToSpeech.QUEUE_FLUSH, new Bundle(), UTTERANCE_ID);
    }

    public void stop()
    {
        if (tts != null) tts.stop();
    }

    public static void shutdown()
    {
        TTSBridge current = instance;
        if (current == null) return;
        instance = null;
        try
        {
            current.tts.stop();
            current.tts.shutdown();
        }
        catch (Exception ignored)
        {
        }
    }

    private static float clampRate(float rate)
    {
        if (rate <= 0f) return 1.0f;
        return Math.max(0.25f, Math.min(3.0f, rate));
    }
}

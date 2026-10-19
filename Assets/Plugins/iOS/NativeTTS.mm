#import <AVFoundation/AVFoundation.h>

// Native iOS TTS bridge for Unity. Unity compiles this file from
// Assets/Plugins/iOS into the Xcode project automatically; Unity calls
// these functions via DllImport("__Internal").

static AVSpeechSynthesizer *synthesizer = nil;
static AVSpeechSynthesisVoice *voice = nil;
static float speechRate = AVSpeechUtteranceDefaultSpeechRate; // 0.5

extern "C"
{

void LovieTTS_Init(void)
{
    if (synthesizer == nil)
    {
        synthesizer = [[AVSpeechSynthesizer alloc] init];
    }
}

BOOL LovieTTS_IsReady(void)
{
    return synthesizer != nil;
}

BOOL LovieTTS_IsSpeaking(void)
{
    return synthesizer != nil && [synthesizer isSpeaking];
}

// Returns 0 when a voice is installed for the tag, -2 when none is.
int LovieTTS_SetLanguage(const char *languageTag)
{
    if (languageTag == NULL) return -2;

    NSString *tag = [NSString stringWithUTF8String:languageTag];
    AVSpeechSynthesisVoice *match = [AVSpeechSynthesisVoice voiceWithLanguage:tag];

    if (match == nil && [tag length] > 2)
    {
        // Fall back to any installed voice sharing the language code ("en" for "en-US").
        NSString *languageCode = [[tag componentsSeparatedByString:@"-"] firstObject];
        for (AVSpeechSynthesisVoice *candidate in [AVSpeechSynthesisVoice speechVoices])
        {
            if ([candidate.language hasPrefix:languageCode])
            {
                match = candidate;
                break;
            }
        }
    }

    if (match == nil) return -2;

    voice = match;
    return 0;
}

// Unity-friendly rate: 1.0 = normal, 0.5 = half speed, 2.0 = double.
void LovieTTS_SetSpeechRate(float rate)
{
    if (rate <= 0.0f) rate = 1.0f;
    rate = 0.5f * rate; // 1.0 maps onto the AVSpeech default rate
    speechRate = MAX(AVSpeechUtteranceMinimumSpeechRate, MIN(AVSpeechUtteranceMaximumSpeechRate, rate));
}

// Interrupts any current utterance and speaks the new text.
void LovieTTS_Speak(const char *text)
{
    if (synthesizer == nil || text == NULL) return;

    NSString *string = [NSString stringWithUTF8String:text];
    if ([string length] == 0) return;

    [synthesizer stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];

    AVSpeechUtterance *utterance = [AVSpeechUtterance speechUtteranceWithString:string];
    if (voice != nil) utterance.voice = voice;
    utterance.rate = speechRate;
    utterance.pitchMultiplier = 1.0;
    [synthesizer speakUtterance:utterance];
}

void LovieTTS_Stop(void)
{
    [synthesizer stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
}

void LovieTTS_Shutdown(void)
{
    [synthesizer stopSpeakingAtBoundary:AVSpeechBoundaryImmediate];
    synthesizer = nil;
    voice = nil;
}

} // extern "C"

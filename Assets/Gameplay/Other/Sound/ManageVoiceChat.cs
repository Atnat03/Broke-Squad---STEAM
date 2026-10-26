using MetaVoiceChat.Input;

public class ManageVoiceChat : VcInputFilter
{
    protected override void Filter(int index, ref float[] samples)
    {
        if (!VoiceChatState.Activated)
            samples = null;
    }
}
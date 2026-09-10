namespace UI
{
    // Story handed from the stories list card to the quiz manage panel.
    // Set on Create Quiz click, read by the quiz panel controller on enable.
    public static class AdminQuizContext
    {
        public static string SelectedStoryId;
        public static string SelectedStoryTitle;

        public static void Clear()
        {
            SelectedStoryId = null;
            SelectedStoryTitle = null;
        }
    }
}

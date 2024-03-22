namespace UnityEngine.UI.Extensions.Examples
{
    public class PauseMenu : SimpleMenu<PauseMenu>
    {
        public void OnQuitPressed()
        {
            Hide();
            Destroy(gameObject); // This menu does not automatically destroy itself

            GameMenu.Hide();
        }
    }
}
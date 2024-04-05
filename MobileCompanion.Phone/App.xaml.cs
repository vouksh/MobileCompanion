using UraniumUI.Material.Resources;

namespace MobileCompanion.Phone
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            MainPage = new AppShell();
        }
    }
}

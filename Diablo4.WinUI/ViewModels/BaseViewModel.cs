namespace Diablo4.WinUI.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        public BaseViewModel()
        {

        }

        [ObservableProperty]
        private string _title = string.Empty;
    }
}

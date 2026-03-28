namespace Diablo4.WinUI.ViewModels
{
    public partial class BaseViewModel : ObservableObject
    {
        public BaseViewModel()
        {

        }

        [ObservableProperty]
        public partial string Title { get; set; } = string.Empty;
    }
}

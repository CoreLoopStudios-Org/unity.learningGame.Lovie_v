using System.Threading.Tasks;
using UnityEngine;

namespace UI
{
    public sealed class ProfileUploadStatusView : MonoBehaviour
    {
        [SerializeField] private GameObject _root;
        [SerializeField] private GameObject _loadingState;
        [SerializeField] private GameObject _successState;
        [SerializeField] private float _successVisibleSeconds = 1f;

        public async Task ShowLoadingAsync()
        {
            if (_root != null) _root.SetActive(true);
            SetState(loading: true);
            await Task.Yield();
        }

        public async Task ShowSuccessAsync()
        {
            SetState(loading: false);
            await Task.Delay(System.TimeSpan.FromSeconds(_successVisibleSeconds));
            Hide();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
        }

        private void SetState(bool loading)
        {
            if (_loadingState != null) _loadingState.SetActive(loading);
            if (_successState != null) _successState.SetActive(!loading);
        }
    }
}

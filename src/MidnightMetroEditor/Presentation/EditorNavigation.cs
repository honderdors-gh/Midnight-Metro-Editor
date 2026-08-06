namespace MidnightMetroEditor.Presentation;

public sealed class EditorNavigation
{
    readonly List<string> _history = new();
    int _index = -1;
    bool _suppress;

    public event Action<string>? NavigateRequested;

    public bool CanGoBack => _index > 0;
    public bool CanGoForward => _index >= 0 && _index < _history.Count - 1;

    public void Navigate(string viewName, bool recordHistory = true)
    {
        if (string.IsNullOrWhiteSpace(viewName))
            return;

        if (recordHistory)
        {
            if (_index >= 0 && _index < _history.Count - 1)
                _history.RemoveRange(_index + 1, _history.Count - _index - 1);

            if (_history.Count == 0 || _history[^1] != viewName)
            {
                _history.Add(viewName);
                _index = _history.Count - 1;
            }
        }

        _suppress = true;
        try
        {
            NavigateRequested?.Invoke(viewName);
        }
        finally
        {
            _suppress = false;
        }
    }

    public void GoBack()
    {
        if (!CanGoBack)
            return;

        _index--;
        Navigate(_history[_index], recordHistory: false);
    }

    public void GoForward()
    {
        if (!CanGoForward)
            return;

        _index++;
        Navigate(_history[_index], recordHistory: false);
    }

    public void Reset(string viewName)
    {
        _history.Clear();
        _history.Add(viewName);
        _index = 0;
    }

    public bool IsRecording => !_suppress;
}

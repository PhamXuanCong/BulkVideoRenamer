using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using BulkVideoRenamer.Core;
using BulkVideoRenamer.Core.Models;

namespace BulkVideoRenamer.Gui;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<RenamePreviewRow> _rows = [];
    private List<RenameItem> _currentPlan = [];

    public MainWindow()
    {
        InitializeComponent();
        PreviewGrid.ItemsSource = _rows;
    }

    private void ChooseFolder_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "Chọn folder chứa video"
        };

        if (dialog.ShowDialog(this) == true)
        {
            FolderTextBox.Text = dialog.FolderName;
            RefreshPreview();
        }
    }

    private void RefreshInputs_Changed(object sender, TextChangedEventArgs e) => RefreshPreview();

    private void RefreshPreview()
    {
        _rows.Clear();
        _currentPlan = [];
        StatusText.Text = "";

        var folder = FolderTextBox.Text;
        if (string.IsNullOrWhiteSpace(folder))
        {
            RenameButton.IsEnabled = false;
            return;
        }

        try
        {
            _currentPlan = RenameService.BuildPlan(folder, HashtagTextBox.Text);
            foreach (var item in _currentPlan)
                _rows.Add(new RenamePreviewRow(item));

            RenameButton.IsEnabled = _currentPlan.Count > 0;
            StatusText.Text = _currentPlan.Count == 0
                ? "Không tìm thấy file video nào trong folder này."
                : $"{_currentPlan.Count} file sẽ được đổi tên.";
        }
        catch (Exception ex) when (ex is DirectoryNotFoundException or UnauthorizedAccessException)
        {
            RenameButton.IsEnabled = false;
            StatusText.Text = $"Lỗi: {ex.Message}";
        }
    }

    private void Rename_Click(object sender, RoutedEventArgs e)
    {
        if (_currentPlan.Count == 0)
            return;

        var confirm = MessageBox.Show(
            $"Đổi tên {_currentPlan.Count} file trong \"{FolderTextBox.Text}\"?",
            "Xác nhận đổi tên hàng loạt",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            var result = RenameService.Execute(_currentPlan, FolderTextBox.Text);
            StatusText.Text =
                $"Hoàn tất: {result.SucceededCount} đổi tên thành công, " +
                $"{result.SkippedCount} bỏ qua, {result.ErrorCount} lỗi.";
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Không có quyền ghi vào folder:\n{ex.Message}", "Lỗi",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            RefreshPreview();
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        FolderTextBox.Text = "";
        HashtagTextBox.Text = "";
        _rows.Clear();
        _currentPlan = [];
        RenameButton.IsEnabled = false;
        StatusText.Text = "";
    }

    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        var folder = FolderTextBox.Text;
        if (string.IsNullOrWhiteSpace(folder))
        {
            MessageBox.Show("Chọn folder trước khi Undo.", "Thiếu folder",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            "Khôi phục lần đổi tên gần nhất trong folder này?",
            "Xác nhận Undo",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (confirm != MessageBoxResult.Yes)
            return;

        try
        {
            var result = RenameLogger.Undo(folder);
            StatusText.Text = $"Undo hoàn tất: {result.SucceededCount} file khôi phục, {result.ErrorCount} lỗi.";
        }
        catch (FileNotFoundException ex)
        {
            MessageBox.Show(ex.Message, "Không có gì để Undo",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        finally
        {
            RefreshPreview();
        }
    }
}

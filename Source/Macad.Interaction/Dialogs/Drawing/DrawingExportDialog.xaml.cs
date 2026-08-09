using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using Macad.Core;
using Macad.Core.Drawing;
using Macad.Presentation;

namespace Macad.Interaction.Dialogs;

public partial class DrawingExportDialog : Dialog
{
    #region Properties

    public Drawing Drawing
    {
        get { return (Drawing)GetValue(DrawingProperty); }
        set { SetValue(DrawingProperty, value); }
    }

    public static readonly DependencyProperty DrawingProperty =
        DependencyProperty.Register(nameof(Drawing), typeof(Drawing), typeof(DrawingExportDialog));

    //--------------------------------------------------------------------------------------------------

    public DrawingExportPanelBase ExportPanel
    {
        get { return (DrawingExportPanelBase)GetValue(ExportPanelProperty); }
        set { SetValue(ExportPanelProperty, value); }
    }

    public static readonly DependencyProperty ExportPanelProperty =
        DependencyProperty.Register(nameof(ExportPanel), typeof(DrawingExportPanelBase), typeof(DrawingExportDialog));

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region C'tor and factory

    public DrawingExportDialog(Window ownerWindow, DrawingExportPanelBase panel)
    {
        Owner = ownerWindow;
        ExportPanel = panel;
        Drawing = ExportPanel.Drawing;

        ExportPanel.PropertyChanged += _ExportPanel_PropertyChanged;
        ExportCommand = new RelayCommand(_ExecuteExport);

        InitializeComponent();
    }

    //--------------------------------------------------------------------------------------------------

    public static bool Execute(Window ownerWindow, DrawingExportPanelBase panel)
    {
        var dlg = new DrawingExportDialog(ownerWindow, panel);
        return dlg.ShowDialog();
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Callbacks

    //--------------------------------------------------------------------------------------------------

    protected override void OnClosed(EventArgs e)
    {
        ExportPanel.PropertyChanged -= _ExportPanel_PropertyChanged;
        base.OnClosed(e);
    }

    //--------------------------------------------------------------------------------------------------

    void _ExportPanel_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName == nameof(ExportPanel.Drawing))
        {
            // Force update of the rendering, even when the drawing instance is the same
            Drawing = null;
            Drawing = ExportPanel.Drawing;
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

    #region Commands

    public ICommand ExportCommand { get; }

    //--------------------------------------------------------------------------------------------------

    void _ExecuteExport()
    {
        if (ExportPanel == null)
        {
            return;
        }

        ExportPanel.CommitSettings();

        if (!ExportDialog.Execute<IDrawingExporter>(out string fileName, out var exporter))
        {
            return;
        }

        using (new ProcessingScope(null, "Exporting Drawing"))
        {
            if (!_DoExport(fileName, exporter))
            {
                MessageBox.Show(this, "The export was not successful. Please see message log for further information about the error.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }

        DialogResult = true;
    }

    //--------------------------------------------------------------------------------------------------

    bool _DoExport(string filename, IDrawingExporter exporter)
    {
        try
        {
            return exporter.DoExport(filename, Drawing);
        }
        catch (Exception e)
        {
            Messages.Exception("Exception while exporting drawing.", e);
            return false;
        }
    }

    //--------------------------------------------------------------------------------------------------

    #endregion

}

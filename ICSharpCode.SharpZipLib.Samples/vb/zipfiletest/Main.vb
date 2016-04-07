Imports System
Imports System.Text
Imports System.Collections
Imports System.IO
Imports System.Windows.Forms
Imports ICSharpCode.SharpZipLib.Zip

Public Class Form1
    Inherits System.Windows.Forms.Form
    Friend WithEvents btnTest As System.Windows.Forms.Button
    Friend Label1 As System.Windows.Forms.Label
    Friend chdrSize As System.Windows.Forms.ColumnHeader
    Friend chdrName As System.Windows.Forms.ColumnHeader
    Friend hdrTime As System.Windows.Forms.ColumnHeader
    Friend chdrRawSize As System.Windows.Forms.ColumnHeader
    Friend lvZipContents As System.Windows.Forms.ListView
    Friend lblListName As System.Windows.Forms.Label
    Friend hdrDate As System.Windows.Forms.ColumnHeader
    Friend WithEvents btnBrowse As System.Windows.Forms.Button
    Friend txtFileName As System.Windows.Forms.TextBox

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.

    Public Shared Sub Main()
        Dim fMainForm As New Form1
        fMainForm.ShowDialog()
    End Sub

    Public Sub New()
        MyBase.New()
        ' Must be called for initialization
        Me.InitializeComponent()
        '
        ' TODO : Add constructor code after InitializeComponents
        '
    End Sub

    Private Sub InitializeComponent()
        Me.txtFileName = New System.Windows.Forms.TextBox
        Me.hdrDate = New System.Windows.Forms.ColumnHeader
        Me.lblListName = New System.Windows.Forms.Label
        Me.lvZipContents = New System.Windows.Forms.ListView
        Me.chdrName = New System.Windows.Forms.ColumnHeader
        Me.chdrRawSize = New System.Windows.Forms.ColumnHeader
        Me.chdrSize = New System.Windows.Forms.ColumnHeader
        Me.hdrTime = New System.Windows.Forms.ColumnHeader
        Me.Label1 = New System.Windows.Forms.Label
        Me.btnTest = New System.Windows.Forms.Button
        Me.btnBrowse = New System.Windows.Forms.Button
        Me.SuspendLayout()
        '
        'txtFileName
        '
        Me.txtFileName.Location = New System.Drawing.Point(117, 21)
        Me.txtFileName.Name = "txtFileName"
        Me.txtFileName.Size = New System.Drawing.Size(197, 20)
        Me.txtFileName.TabIndex = 0
        '
        'hdrDate
        '
        Me.hdrDate.Text = "Date"
        Me.hdrDate.Width = 71
        '
        'lblListName
        '
        Me.lblListName.Location = New System.Drawing.Point(16, 49)
        Me.lblListName.Name = "lblListName"
        Me.lblListName.Size = New System.Drawing.Size(256, 21)
        Me.lblListName.TabIndex = 5
        Me.lblListName.Text = "(no file)"
        '
        'lvZipContents
        '
        Me.lvZipContents.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
                    Or System.Windows.Forms.AnchorStyles.Left) _
                    Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.lvZipContents.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.chdrName, Me.chdrRawSize, Me.chdrSize, Me.hdrDate, Me.hdrTime})
        Me.lvZipContents.FullRowSelect = True
        Me.lvZipContents.GridLines = True
        Me.lvZipContents.Location = New System.Drawing.Point(8, 74)
        Me.lvZipContents.Name = "lvZipContents"
        Me.lvZipContents.Size = New System.Drawing.Size(440, 240)
        Me.lvZipContents.Sorting = System.Windows.Forms.SortOrder.Ascending
        Me.lvZipContents.TabIndex = 6
        Me.lvZipContents.UseCompatibleStateImageBehavior = False
        Me.lvZipContents.View = System.Windows.Forms.View.Details
        '
        'chdrName
        '
        Me.chdrName.Text = "Name"
        Me.chdrName.Width = 127
        '
        'chdrRawSize
        '
        Me.chdrRawSize.Text = "RawSize"
        Me.chdrRawSize.Width = 67
        '
        'chdrSize
        '
        Me.chdrSize.Text = "Size"
        Me.chdrSize.Width = 52
        '
        'hdrTime
        '
        Me.hdrTime.Text = "Time"
        Me.hdrTime.Width = 58
        '
        'Label1
        '
        Me.Label1.Location = New System.Drawing.Point(16, 21)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(91, 20)
        Me.Label1.TabIndex = 3
        Me.Label1.Text = "Zip File Name:"
        '
        'btnTest
        '
        Me.btnTest.Location = New System.Drawing.Point(352, 24)
        Me.btnTest.Name = "btnTest"
        Me.btnTest.Size = New System.Drawing.Size(68, 20)
        Me.btnTest.TabIndex = 1
        Me.btnTest.Text = "View"
        '
        'btnBrowse
        '
        Me.btnBrowse.Location = New System.Drawing.Point(320, 21)
        Me.btnBrowse.Name = "btnBrowse"
        Me.btnBrowse.Size = New System.Drawing.Size(26, 23)
        Me.btnBrowse.TabIndex = 7
        Me.btnBrowse.Text = "..."
        Me.btnBrowse.UseVisualStyleBackColor = True
        '
        'Form1
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(456, 334)
        Me.Controls.Add(Me.btnBrowse)
        Me.Controls.Add(Me.lvZipContents)
        Me.Controls.Add(Me.lblListName)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.txtFileName)
        Me.Controls.Add(Me.btnTest)
        Me.Name = "Form1"
        Me.Text = "Test Zip File"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Private Sub btnTest_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnTest.Click
        Dim objEntry As ZipEntry
        Dim dtStamp As DateTime
        Dim objItem As ListViewItem
        Dim zFile As ZipFile

        ' Really simple error handling here (catch all)
        Try
            zFile = New ZipFile(txtFileName.Text)
        Catch Ex As System.Exception
            MessageBox.Show(Ex.Message)
            Exit Sub
        End Try

        lblListName.Text = "Listing of : " + zFile.Name.ToString
        lvZipContents.BeginUpdate()
        lvZipContents.Items.Clear()

        For Each objEntry In zFile
            objItem = New ListViewItem(objEntry.Name)
            dtStamp = objEntry.DateTime
            objItem.SubItems.Add(objEntry.Size.ToString)
            objItem.SubItems.Add(objEntry.CompressedSize.ToString)
            objItem.SubItems.Add(dtStamp.ToString("dd-MM-yy"))
            objItem.SubItems.Add(dtStamp.ToString("t"))
            objItem.SubItems.Add(objEntry.Name.ToString)
            lvZipContents.Items.Add(objItem)
        Next

        lvZipContents.EndUpdate()
    End Sub

    Private Sub btnBrowse_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBrowse.Click
        Dim ofn As New OpenFileDialog()

        ofn.InitialDirectory = "c:\"
        ofn.Filter = "Zip Files (*.zip)|*.zip|All files (*.*)|*.*"

        If (ofn.ShowDialog() = Windows.Forms.DialogResult.OK) Then
            txtFileName.Text = ofn.FileName
        End If
    End Sub
End Class


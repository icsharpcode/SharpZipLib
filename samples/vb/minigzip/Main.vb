Imports System
Imports System.IO
Imports System.ComponentModel
Imports ICSharpCode.SharpZipLib.GZip

Public Class Form1
    Inherits System.Windows.Forms.Form

#Region " Windows Form Designer generated code "

    Public Sub New()
        MyBase.New()

        'This call is required by the Windows Form Designer.
        InitializeComponent()

        'Add any initialization after the InitializeComponent() call

    End Sub

    'Form overrides dispose to clean up the component list.
    Protected Overloads Overrides Sub Dispose(ByVal disposing As Boolean)
        If disposing Then
            If Not (components Is Nothing) Then
                components.Dispose()
            End If
        End If
        MyBase.Dispose(disposing)
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    Friend WithEvents GroupBox1 As System.Windows.Forms.GroupBox
    Friend WithEvents Label2 As System.Windows.Forms.Label
    Friend WithEvents txtGzipFile As System.Windows.Forms.TextBox
    Friend WithEvents btnGzipBrowse As System.Windows.Forms.Button
    Friend WithEvents btnGzip As System.Windows.Forms.Button
    Friend WithEvents GroupBox2 As System.Windows.Forms.GroupBox
    Friend WithEvents btnGunzipBrowse As System.Windows.Forms.Button
    Friend WithEvents txtGunzipFile As System.Windows.Forms.TextBox
    Friend WithEvents Label3 As System.Windows.Forms.Label
    Friend WithEvents btnGunzip As System.Windows.Forms.Button
    <System.Diagnostics.DebuggerStepThrough()> Private Sub InitializeComponent()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.btnGzip = New System.Windows.Forms.Button()
        Me.btnGzipBrowse = New System.Windows.Forms.Button()
        Me.txtGzipFile = New System.Windows.Forms.TextBox()
        Me.Label2 = New System.Windows.Forms.Label()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.btnGunzip = New System.Windows.Forms.Button()
        Me.btnGunzipBrowse = New System.Windows.Forms.Button()
        Me.txtGunzipFile = New System.Windows.Forms.TextBox()
        Me.Label3 = New System.Windows.Forms.Label()
        Me.GroupBox1.SuspendLayout()
        Me.GroupBox2.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.Controls.AddRange(New System.Windows.Forms.Control() {Me.btnGzip, Me.btnGzipBrowse, Me.txtGzipFile, Me.Label2})
        Me.GroupBox1.Location = New System.Drawing.Point(8, 8)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Size = New System.Drawing.Size(344, 96)
        Me.GroupBox1.TabIndex = 7
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Text = "Demo gzip-ing"
        '
        'btnGzip
        '
        Me.btnGzip.Location = New System.Drawing.Point(184, 56)
        Me.btnGzip.Name = "btnGzip"
        Me.btnGzip.TabIndex = 3
        Me.btnGzip.Text = "Gzip file"
        '
        'btnGzipBrowse
        '
        Me.btnGzipBrowse.Location = New System.Drawing.Point(72, 56)
        Me.btnGzipBrowse.Name = "btnGzipBrowse"
        Me.btnGzipBrowse.Size = New System.Drawing.Size(104, 23)
        Me.btnGzipBrowse.TabIndex = 2
        Me.btnGzipBrowse.Text = "Browse for file..."
        '
        'txtGzipFile
        '
        Me.txtGzipFile.Location = New System.Drawing.Point(72, 24)
        Me.txtGzipFile.Name = "txtGzipFile"
        Me.txtGzipFile.Size = New System.Drawing.Size(264, 20)
        Me.txtGzipFile.TabIndex = 1
        Me.txtGzipFile.Text = ""
        '
        'Label2
        '
        Me.Label2.Location = New System.Drawing.Point(16, 24)
        Me.Label2.Name = "Label2"
        Me.Label2.Size = New System.Drawing.Size(56, 16)
        Me.Label2.TabIndex = 0
        Me.Label2.Text = "Filename:"
        '
        'GroupBox2
        '
        Me.GroupBox2.Controls.AddRange(New System.Windows.Forms.Control() {Me.btnGunzip, Me.btnGunzipBrowse, Me.txtGunzipFile, Me.Label3})
        Me.GroupBox2.Location = New System.Drawing.Point(8, 120)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Size = New System.Drawing.Size(344, 96)
        Me.GroupBox2.TabIndex = 8
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Text = "Demo gunzip-ing"
        '
        'btnGunzip
        '
        Me.btnGunzip.Location = New System.Drawing.Point(184, 56)
        Me.btnGunzip.Name = "btnGunzip"
        Me.btnGunzip.TabIndex = 3
        Me.btnGunzip.Text = "GUnZip file"
        '
        'btnGunzipBrowse
        '
        Me.btnGunzipBrowse.Location = New System.Drawing.Point(72, 56)
        Me.btnGunzipBrowse.Name = "btnGunzipBrowse"
        Me.btnGunzipBrowse.Size = New System.Drawing.Size(104, 23)
        Me.btnGunzipBrowse.TabIndex = 2
        Me.btnGunzipBrowse.Text = "Browse for file..."
        '
        'txtGunzipFile
        '
        Me.txtGunzipFile.Location = New System.Drawing.Point(72, 24)
        Me.txtGunzipFile.Name = "txtGunzipFile"
        Me.txtGunzipFile.Size = New System.Drawing.Size(264, 20)
        Me.txtGunzipFile.TabIndex = 1
        Me.txtGunzipFile.Text = ""
        '
        'Label3
        '
        Me.Label3.Location = New System.Drawing.Point(16, 24)
        Me.Label3.Name = "Label3"
        Me.Label3.Size = New System.Drawing.Size(56, 16)
        Me.Label3.TabIndex = 0
        Me.Label3.Text = "Filename:"
        '
        'Form1
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
        Me.ClientSize = New System.Drawing.Size(360, 229)
        Me.Controls.AddRange(New System.Windows.Forms.Control() {Me.GroupBox1, Me.GroupBox2})
        Me.Name = "Form1"
        Me.Text = "Mini GZip Demo"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox2.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

#End Region

    Private Sub btnGzipBrowse_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGzipBrowse.Click
        Dim ofn As New OpenFileDialog()

        ofn.InitialDirectory = "c:\"
        ofn.Filter = "All files (*.*)|*.*"

        If ofn.ShowDialog() = DialogResult.OK Then
            txtGzipFile.Text = ofn.FileName
        End If
    End Sub

    Private Sub btnGzip_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGzip.Click
        Dim stmGzipArchive As Stream = New GZipOutputStream(File.Create(txtGzipFile.Text + ".gz"))
        Dim stmInputFile As FileStream = File.OpenRead(txtGzipFile.Text)

        Dim nFileStreamLength As Int32
        nFileStreamLength = stmInputFile.Length
        Dim abyWriteData(nFileStreamLength) As Byte

        stmInputFile.Read(abyWriteData, 0, nFileStreamLength)
        stmGzipArchive.Write(abyWriteData, 0, nFileStreamLength)

        stmGzipArchive.Flush()
        stmGzipArchive.Close()
        stmInputFile.Close()
    End Sub

    Private Sub btnGunzipBrowse_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGunzipBrowse.Click
        Dim ofn As New OpenFileDialog()

        ofn.InitialDirectory = "c:\"
        ofn.Filter = "GZip files (*.gz)|*.gz"

        If ofn.ShowDialog() = DialogResult.OK Then
            txtGunzipFile.Text = ofn.FileName
        End If
    End Sub

    Private Sub btnGunzip_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnGunzip.Click
        Dim strDestinationFile As String
        Dim nSize As Integer = 2048
        Dim nSizeRead As Integer
        Dim abyWriteData(2048) As Byte

        strDestinationFile = Path.GetDirectoryName(txtGunzipFile.Text) & _
                Path.GetFileNameWithoutExtension(txtGunzipFile.Text)

        Dim stmGzipArchive As Stream = New GZipInputStream(File.OpenRead(txtGunzipFile.Text))
        Dim stmDestinationFile As FileStream = File.Create(strDestinationFile)

        While (True)
            nSizeRead = stmGzipArchive.Read(abyWriteData, 0, nSize)
            If nSizeRead > 0 Then
                stmDestinationFile.Write(abyWriteData, 0, nSizeRead)
            Else
                Exit While
            End If
        End While

        stmDestinationFile.Flush()
        stmDestinationFile.Close()

        stmGzipArchive.Close()
    End Sub
End Class

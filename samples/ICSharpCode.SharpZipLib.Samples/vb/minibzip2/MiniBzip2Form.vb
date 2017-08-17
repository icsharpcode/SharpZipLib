Imports System
Imports System.Windows.Forms
Imports System.IO

Imports ICSharpCode.SharpZipLib.BZip2

Public Class MiniBzip2Form
	Inherits System.Windows.Forms.Form
	Friend txtFileName As System.Windows.Forms.TextBox
	Friend WithEvents btnBrowseForBZ As System.Windows.Forms.Button
	Friend Label1 As System.Windows.Forms.Label
	Friend rdCompress As System.Windows.Forms.RadioButton
	Friend GroupBox1 As System.Windows.Forms.GroupBox
	Friend WithEvents btnExecute As System.Windows.Forms.Button
	Friend rdDecompress As System.Windows.Forms.RadioButton


#Region " Windows Form Designer generated code "

	Public Shared Sub Main()
		Dim fMainForm As New MiniBzip2Form
		fMainForm.ShowDialog
	End Sub

	Public Sub New()
		MyBase.New()

		'This call is required by the Windows Form Designer.
		InitializeComponent

		'Add any initialization after the InitializeComponent() call

	End Sub

	Private Sub InitializeComponent()
		Me.rdDecompress = New System.Windows.Forms.RadioButton()
		Me.btnExecute = New System.Windows.Forms.Button()
		Me.GroupBox1 = New System.Windows.Forms.GroupBox()
		Me.rdCompress = New System.Windows.Forms.RadioButton()
		Me.Label1 = New System.Windows.Forms.Label()
		Me.btnBrowseForBZ = New System.Windows.Forms.Button()
		Me.txtFileName = New System.Windows.Forms.TextBox()
		Me.GroupBox1.SuspendLayout
		Me.SuspendLayout
		'
		'rdDecompress
		'
		Me.rdDecompress.Location = New System.Drawing.Point(14, 108)
		Me.rdDecompress.Name = "rdDecompress"
		Me.rdDecompress.Size = New System.Drawing.Size(188, 41)
		Me.rdDecompress.TabIndex = 1
		Me.rdDecompress.Text = "decompress"
		'
		'btnExecute
		'
		Me.btnExecute.Location = New System.Drawing.Point(389, 190)
		Me.btnExecute.Name = "btnExecute"
		Me.btnExecute.Size = New System.Drawing.Size(201, 37)
		Me.btnExecute.TabIndex = 2
		Me.btnExecute.Text = "Execute"
		'
		'GroupBox1
		'
		Me.GroupBox1.Controls.Add(Me.rdDecompress)
		Me.GroupBox1.Controls.Add(Me.rdCompress)
		Me.GroupBox1.Location = New System.Drawing.Point(14, 68)
		Me.GroupBox1.Name = "GroupBox1"
		Me.GroupBox1.Size = New System.Drawing.Size(288, 162)
		Me.GroupBox1.TabIndex = 3
		Me.GroupBox1.TabStop = false
		Me.GroupBox1.Text = "Operation to perform"
		'
		'rdCompress
		'
		Me.rdCompress.Checked = true
		Me.rdCompress.Location = New System.Drawing.Point(14, 54)
		Me.rdCompress.Name = "rdCompress"
		Me.rdCompress.Size = New System.Drawing.Size(188, 41)
		Me.rdCompress.TabIndex = 0
		Me.rdCompress.TabStop = true
		Me.rdCompress.Text = "compress"
		'
		'Label1
		'
		Me.Label1.Location = New System.Drawing.Point(14, 27)
		Me.Label1.Name = "Label1"
		Me.Label1.Size = New System.Drawing.Size(130, 27)
		Me.Label1.TabIndex = 4
		Me.Label1.Text = "Filename:"
		'
		'btnBrowseForBZ
		'
		Me.btnBrowseForBZ.Location = New System.Drawing.Point(547, 27)
		Me.btnBrowseForBZ.Name = "btnBrowseForBZ"
		Me.btnBrowseForBZ.Size = New System.Drawing.Size(43, 37)
		Me.btnBrowseForBZ.TabIndex = 5
		Me.btnBrowseForBZ.Text = "..."
		'
		'txtFileName
		'
		Me.txtFileName.Location = New System.Drawing.Point(173, 27)
		Me.txtFileName.Name = "txtFileName"
		Me.txtFileName.Size = New System.Drawing.Size(360, 29)
		Me.txtFileName.TabIndex = 0
		'
		'MainForm
		'
		Me.AutoScaleBaseSize = New System.Drawing.Size(9, 22)
		Me.ClientSize = New System.Drawing.Size(690, 341)
		Me.Controls.Add(Me.btnBrowseForBZ)
		Me.Controls.Add(Me.Label1)
		Me.Controls.Add(Me.GroupBox1)
		Me.Controls.Add(Me.btnExecute)
		Me.Controls.Add(Me.txtFileName)
		Me.Name = "MainForm"
		Me.Text = "Mini BZ2 Application"
		Me.GroupBox1.ResumeLayout(false)
		Me.ResumeLayout(false)
		Me.PerformLayout

End Sub

#End Region

	Private Sub btnExecuteClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnExecute.Click
		' Simple input sanity checks
		If (0 = txtFileName.Text.Length)
			MessageBox.Show("Please enter a file name", "File name is missing")
			Return
		End If

		If Not File.Exists(txtFileName.Text)
			MessageBox.Show(txtFileName.Text, "Cannot open file")
			Return
		End If


		If (False = rdCompress.Checked) Then
			' Decompression of single-file archive
			Dim fsBZ2Archive As FileStream, fsOutput As FileStream
			Dim strOutputFilename As String

			fsBZ2Archive = File.OpenRead(txtFileName.Text)
			strOutputFilename = Path.GetDirectoryName(txtFileName.Text) & _
				Path.GetFileNameWithoutExtension(txtFileName.Text)

			fsOutput = File.Create(strOutputFilename)

            BZip2.BZip2.Decompress(fsBZ2Archive, fsOutput, False)

            fsBZ2Archive.Close()
			fsOutput.Flush()
			fsOutput.Close()
		Else
			'Compression of single-file archive
			Dim fsInputFile As FileStream, fsBZ2Archive As FileStream
			fsInputFile = File.OpenRead(txtFileName.Text)
			fsBZ2Archive = File.Create(txtFileName.Text + ".bz")

            BZip2.BZip2.Compress(fsInputFile, fsBZ2Archive, False, 4026)

            fsInputFile.Close()
			' fsBZ2Archive.Flush() & fsBZ2Archive.Close() are automatically called by .Compress
		End If
	End Sub

	Private Sub btnBrowseForBZClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBrowseForBZ.Click
		Dim ofn As New OpenFileDialog()

		ofn.InitialDirectory = "c:\"
		ofn.Filter = "BZ files (*.bz)|*.bz|All files (*.*)|*.*"

		If (ofn.ShowDialog() = Windows.Forms.DialogResult.OK) Then
			txtFileName.Text = ofn.FileName
		End If
	End Sub
End Class

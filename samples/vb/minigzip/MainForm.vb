'
' Created by SharpDevelop.
' User: JohnR
' Date: 17/01/2005
' Time: 10:57 p.m.
' 
' To change this template use Tools | Options | Coding | Edit Standard Headers.
'
Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms

Imports ICSharpCode.SharpZipLib.GZip

Namespace MiniGzip
	
	Public Class MainForm
		Inherits System.Windows.Forms.Form
		Private txtGunzipFile As System.Windows.Forms.TextBox
		Private txtGzipFile As System.Windows.Forms.TextBox
		Private fileNameLabel As System.Windows.Forms.Label
		Private btnGzipBrowse As System.Windows.Forms.Button
		Private GroupBox2 As System.Windows.Forms.GroupBox
		Private btnGunzipBrowse As System.Windows.Forms.Button
		Private btnGunzip As System.Windows.Forms.Button
		Private btnGzip As System.Windows.Forms.Button
		Private gzipGroupBox As System.Windows.Forms.GroupBox
		Private Label3 As System.Windows.Forms.Label
		
		Public Shared Sub Main
			Dim fMainForm As New MainForm
			fMainForm.ShowDialog()
		End Sub
		
		Public Sub New()
			MyBase.New
			'
			' The Me.InitializeComponent call is required for Windows Forms designer support.
			'
			Me.InitializeComponent
			'
			' TODO : Add constructor code after InitializeComponents
			'
		End Sub
		
		#Region " Windows Forms Designer generated code "
		' This method is required for Windows Forms designer support.
		' Do not change the method contents inside the source code editor. The Forms designer might
		' not be able to load this method if it was changed manually.
		Private Sub InitializeComponent()
			Me.Label3 = New System.Windows.Forms.Label
			Me.gzipGroupBox = New System.Windows.Forms.GroupBox
			Me.btnGzip = New System.Windows.Forms.Button
			Me.btnGunzip = New System.Windows.Forms.Button
			Me.btnGunzipBrowse = New System.Windows.Forms.Button
			Me.GroupBox2 = New System.Windows.Forms.GroupBox
			Me.btnGzipBrowse = New System.Windows.Forms.Button
			Me.fileNameLabel = New System.Windows.Forms.Label
			Me.txtGzipFile = New System.Windows.Forms.TextBox
			Me.txtGunzipFile = New System.Windows.Forms.TextBox
			Me.gzipGroupBox.SuspendLayout
			Me.GroupBox2.SuspendLayout
			Me.SuspendLayout
			'
			'Label3
			'
			Me.Label3.Location = New System.Drawing.Point(16, 24)
			Me.Label3.Name = "Label3"
			Me.Label3.Size = New System.Drawing.Size(56, 16)
			Me.Label3.TabIndex = 0
			Me.Label3.Text = "Filename:"
			'
			'gzipGroupBox
			'
			Me.gzipGroupBox.Controls.Add(Me.btnGzip)
			Me.gzipGroupBox.Controls.Add(Me.btnGzipBrowse)
			Me.gzipGroupBox.Controls.Add(Me.txtGzipFile)
			Me.gzipGroupBox.Controls.Add(Me.fileNameLabel)
			Me.gzipGroupBox.Location = New System.Drawing.Point(16, 16)
			Me.gzipGroupBox.Name = "gzipGroupBox"
			Me.gzipGroupBox.Size = New System.Drawing.Size(368, 120)
			Me.gzipGroupBox.TabIndex = 0
			Me.gzipGroupBox.TabStop = false
			Me.gzipGroupBox.Text = "Demo Gzip"
			'
			'btnGzip
			'
			Me.btnGzip.Location = New System.Drawing.Point(264, 56)
			Me.btnGzip.Name = "btnGzip"
			Me.btnGzip.TabIndex = 4
			Me.btnGzip.Text = "Gzip file"
			AddHandler Me.btnGzip.Click, AddressOf Me.BtnGzipClick
			'
			'btnGunzip
			'
			Me.btnGunzip.Location = New System.Drawing.Point(264, 56)
			Me.btnGunzip.Name = "btnGunzip"
			Me.btnGunzip.TabIndex = 3
			Me.btnGunzip.Text = "GUnZip file"
			AddHandler Me.btnGunzip.Click, AddressOf Me.BtnGunzipClick
			'
			'btnGunzipBrowse
			'
			Me.btnGunzipBrowse.Location = New System.Drawing.Point(80, 56)
			Me.btnGunzipBrowse.Name = "btnGunzipBrowse"
			Me.btnGunzipBrowse.Size = New System.Drawing.Size(104, 23)
			Me.btnGunzipBrowse.TabIndex = 2
			Me.btnGunzipBrowse.Text = "Browse for file..."
			AddHandler Me.btnGunzipBrowse.Click, AddressOf Me.BtnGunzipBrowseClick
			'
			'GroupBox2
			'
			Me.GroupBox2.Controls.Add(Me.btnGunzip)
			Me.GroupBox2.Controls.Add(Me.btnGunzipBrowse)
			Me.GroupBox2.Controls.Add(Me.txtGunzipFile)
			Me.GroupBox2.Controls.Add(Me.Label3)
			Me.GroupBox2.Location = New System.Drawing.Point(16, 144)
			Me.GroupBox2.Name = "GroupBox2"
			Me.GroupBox2.Size = New System.Drawing.Size(368, 96)
			Me.GroupBox2.TabIndex = 9
			Me.GroupBox2.TabStop = false
			Me.GroupBox2.Text = "Demo Gunzip"
			'
			'btnGzipBrowse
			'
			Me.btnGzipBrowse.Location = New System.Drawing.Point(80, 56)
			Me.btnGzipBrowse.Name = "btnGzipBrowse"
			Me.btnGzipBrowse.Size = New System.Drawing.Size(104, 23)
			Me.btnGzipBrowse.TabIndex = 3
			Me.btnGzipBrowse.Text = "Browse for file..."
			AddHandler Me.btnGzipBrowse.Click, AddressOf Me.BtnGzipBrowseClick
			'
			'fileNameLabel
			'
			Me.fileNameLabel.Location = New System.Drawing.Point(16, 24)
			Me.fileNameLabel.Name = "fileNameLabel"
			Me.fileNameLabel.Size = New System.Drawing.Size(56, 16)
			Me.fileNameLabel.TabIndex = 1
			Me.fileNameLabel.Text = "Filename:"
			'
			'txtGzipFile
			'
			Me.txtGzipFile.Location = New System.Drawing.Point(80, 16)
			Me.txtGzipFile.Name = "txtGzipFile"
			Me.txtGzipFile.Size = New System.Drawing.Size(264, 21)
			Me.txtGzipFile.TabIndex = 2
			Me.txtGzipFile.Text = ""
			'
			'txtGunzipFile
			'
			Me.txtGunzipFile.Location = New System.Drawing.Point(80, 24)
			Me.txtGunzipFile.Name = "txtGunzipFile"
			Me.txtGunzipFile.Size = New System.Drawing.Size(264, 21)
			Me.txtGunzipFile.TabIndex = 1
			Me.txtGunzipFile.Text = ""
			'
			'MainForm
			'
			Me.AutoScaleBaseSize = New System.Drawing.Size(5, 14)
			Me.ClientSize = New System.Drawing.Size(400, 270)
			Me.Controls.Add(Me.GroupBox2)
			Me.Controls.Add(Me.gzipGroupBox)
			Me.Name = "MainForm"
			Me.Text = "Mini GZip Demo"
			Me.gzipGroupBox.ResumeLayout(false)
			Me.GroupBox2.ResumeLayout(false)
			Me.ResumeLayout(false)
		End Sub
		#End Region
		
		Private Sub BtnGzipBrowseClick(sender As System.Object, e As System.EventArgs)
			Dim ofn As New OpenFileDialog()
			
			ofn.InitialDirectory = "c:\"
			ofn.Filter = "All files (*.*)|*.*"
			
			If ofn.ShowDialog() = DialogResult.OK Then
				txtGzipFile.Text = ofn.FileName
			End If
		End Sub
		
		Private Sub BtnGzipClick(sender As System.Object, e As System.EventArgs)
			If File.Exists(txtGZipFIle.Text) Then
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
			Else
				MessageBox.Show("File doesnt exist")
			End If
			
		End Sub
		
		Private Sub BtnGunzipBrowseClick(sender As System.Object, e As System.EventArgs)
			Dim ofn As New OpenFileDialog()
			
			ofn.InitialDirectory = "c:\"
			ofn.Filter = "All files (*.*)|*.*"
			
			If ofn.ShowDialog() = DialogResult.OK Then
				txtGunzipFile.Text = ofn.FileName
			End If
		End Sub
		
		Private Sub BtnGunzipClick(sender As System.Object, e As System.EventArgs)
			If File.Exists(txtGunzipFile.Text) Then
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
			Else
				MessageBox.Show("File doesnt exist")
			End If
		End Sub
		
	End Class
End Namespace

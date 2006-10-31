'
' Created by SharpDevelop.
' User: JohnR
' Date: 1/11/2006
' Time: 5:58 a.m.
' 
' To change this template use Tools | Options | Coding | Edit Standard Headers.
'
Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.IO
Imports ICSharpCode.SharpZipLib.Zip

Namespace CreateZipFile
	
	Public Class MainForm
		Inherits System.Windows.Forms.Form
		Private btnZipIt As System.Windows.Forms.Button
		Private label2 As System.Windows.Forms.Label
		Private label1 As System.Windows.Forms.Label
		Private txtSourceDir As System.Windows.Forms.TextBox
		Private folderBrowserDialog As System.Windows.Forms.FolderBrowserDialog
		Private btnBrowseForFolder As System.Windows.Forms.Button
		Private txtZipFileName As System.Windows.Forms.TextBox
		
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
			Me.txtZipFileName = New System.Windows.Forms.TextBox
			Me.btnBrowseForFolder = New System.Windows.Forms.Button
			Me.folderBrowserDialog = New System.Windows.Forms.FolderBrowserDialog
			Me.txtSourceDir = New System.Windows.Forms.TextBox
			Me.label1 = New System.Windows.Forms.Label
			Me.label2 = New System.Windows.Forms.Label
			Me.btnZipIt = New System.Windows.Forms.Button
			Me.SuspendLayout
			'
			'txtZipFileName
			'
			Me.txtZipFileName.Location = New System.Drawing.Point(24, 24)
			Me.txtZipFileName.Name = "txtZipFileName"
			Me.txtZipFileName.TabIndex = 2
			Me.txtZipFileName.Text = "Demo.zip"
			'
			'btnBrowseForFolder
			'
			Me.btnBrowseForFolder.Location = New System.Drawing.Point(416, 24)
			Me.btnBrowseForFolder.Name = "btnBrowseForFolder"
			Me.btnBrowseForFolder.Size = New System.Drawing.Size(32, 23)
			Me.btnBrowseForFolder.TabIndex = 5
			Me.btnBrowseForFolder.Text = "..."
			AddHandler Me.btnBrowseForFolder.Click, AddressOf Me.BtnBrowseForFolderClick
			'
			'txtSourceDir
			'
			Me.txtSourceDir.Location = New System.Drawing.Point(152, 24)
			Me.txtSourceDir.Name = "txtSourceDir"
			Me.txtSourceDir.Size = New System.Drawing.Size(256, 20)
			Me.txtSourceDir.TabIndex = 0
			Me.txtSourceDir.Text = ""
			'
			'label1
			'
			Me.label1.Location = New System.Drawing.Point(152, 0)
			Me.label1.Name = "label1"
			Me.label1.Size = New System.Drawing.Size(152, 23)
			Me.label1.TabIndex = 3
			Me.label1.Text = "Directory to compress:"
			Me.label1.TextAlign = System.Drawing.ContentAlignment.BottomLeft
			'
			'label2
			'
			Me.label2.Location = New System.Drawing.Point(24, 0)
			Me.label2.Name = "label2"
			Me.label2.TabIndex = 4
			Me.label2.Text = "Zip File name:"
			Me.label2.TextAlign = System.Drawing.ContentAlignment.BottomLeft
			'
			'btnZipIt
			'
			Me.btnZipIt.Location = New System.Drawing.Point(464, 24)
			Me.btnZipIt.Name = "btnZipIt"
			Me.btnZipIt.TabIndex = 1
			Me.btnZipIt.Text = "ZipIt"
			AddHandler Me.btnZipIt.Click, AddressOf Me.BtnZipItClick
			'
			'MainForm
			'
			Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
			Me.ClientSize = New System.Drawing.Size(560, 70)
			Me.Controls.Add(Me.btnBrowseForFolder)
			Me.Controls.Add(Me.label2)
			Me.Controls.Add(Me.label1)
			Me.Controls.Add(Me.txtZipFileName)
			Me.Controls.Add(Me.btnZipIt)
			Me.Controls.Add(Me.txtSourceDir)
			Me.Name = "MainForm"
			Me.Text = "Create Zip File"
			Me.ResumeLayout(false)
		End Sub
		#End Region
		
		Private Sub BtnZipItClick(sender As System.Object, e As System.EventArgs)
			Dim sourceDir As String = txtSourceDir.Text.Trim()
		
			' Simple sanity checks
			If sourceDir.Length = 0
				MessageBox.Show("Please specify a directory")
				Return
			Else
				If Not Directory.Exists(sourceDir)
					MessageBox.Show(sourceDir, "Directory not found")
					Return
				End If
			End If
		
			Dim targetName As String = txtZipFileName.Text.Trim()
			If targetName.Length = 0 Then
				MessageBox.Show("No name specified", "Zip file name error")
				Return
			End If
			
			Dim astrFileNames() As String = Directory.GetFiles(sourceDir)
			Dim strmZipOutputStream As ZipOutputStream
		
			strmZipOutputStream = New ZipOutputStream(File.Create(targetName))

			REM Compression Level: 0-9
			REM 0: no(Compression)
			REM 9: maximum compression
			strmZipOutputStream.SetLevel(9)
		
			Dim strFile As String
		
			For Each strFile In astrFileNames
				Dim strmFile As FileStream = File.OpenRead(strFile)
				Dim abyBuffer(strmFile.Length - 1) As Byte
		
				strmFile.Read(abyBuffer, 0, abyBuffer.Length)
				Dim objZipEntry As ZipEntry = New ZipEntry(strFile)
		
				objZipEntry.DateTime = DateTime.Now
				objZipEntry.Size = strmFile.Length
				strmFile.Close()
				strmZipOutputStream.PutNextEntry(objZipEntry)
				strmZipOutputStream.Write(abyBuffer, 0, abyBuffer.Length)
		
			Next
		
			strmZipOutputStream.Finish()
			strmZipOutputStream.Close()
		
			MessageBox.Show("Operation complete")
		End Sub
		
		Private Sub BtnBrowseForFolderClick(sender As System.Object, e As System.EventArgs)
			If folderBrowserDialog.ShowDialog() = DialogResult.OK
				txtSourceDir.Text = folderBrowserDialog.SelectedPath
			End If
		End Sub
		
	End Class
End Namespace

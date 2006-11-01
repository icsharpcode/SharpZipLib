'
' Created by SharpDevelop.
' User: JohnR
' Date: 1/11/2006
' Time: 9:20 p.m.
' 
' To change this template use Tools | Options | Coding | Edit Standard Headers.
'
Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms

Imports ICSharpCode.SharpZipLib.Core
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
			Me.txtZipFileName.Location = New System.Drawing.Point(16, 40)
			Me.txtZipFileName.Name = "txtZipFileName"
			Me.txtZipFileName.TabIndex = 2
			Me.txtZipFileName.Text = "Demo.Zip"
			'
			'btnBrowseForFolder
			'
			Me.btnBrowseForFolder.Location = New System.Drawing.Point(408, 40)
			Me.btnBrowseForFolder.Name = "btnBrowseForFolder"
			Me.btnBrowseForFolder.Size = New System.Drawing.Size(32, 23)
			Me.btnBrowseForFolder.TabIndex = 4
			Me.btnBrowseForFolder.Text = "..."
			AddHandler Me.btnBrowseForFolder.Click, AddressOf Me.BtnBrowseForFolderClick
			'
			'txtSourceDir
			'
			Me.txtSourceDir.Location = New System.Drawing.Point(144, 40)
			Me.txtSourceDir.Name = "txtSourceDir"
			Me.txtSourceDir.Size = New System.Drawing.Size(256, 21)
			Me.txtSourceDir.TabIndex = 3
			Me.txtSourceDir.Text = ""
			'
			'label1
			'
			Me.label1.Location = New System.Drawing.Point(144, 8)
			Me.label1.Name = "label1"
			Me.label1.Size = New System.Drawing.Size(224, 23)
			Me.label1.TabIndex = 1
			Me.label1.Text = "Source directory:"
			Me.label1.TextAlign = System.Drawing.ContentAlignment.BottomLeft
			'
			'label2
			'
			Me.label2.Location = New System.Drawing.Point(16, 8)
			Me.label2.Name = "label2"
			Me.label2.TabIndex = 0
			Me.label2.Text = "Zip File Name:"
			Me.label2.TextAlign = System.Drawing.ContentAlignment.BottomLeft
			'
			'btnZipIt
			'
			Me.btnZipIt.Location = New System.Drawing.Point(448, 40)
			Me.btnZipIt.Name = "btnZipIt"
			Me.btnZipIt.TabIndex = 5
			Me.btnZipIt.Text = "ZipIt"
			AddHandler Me.btnZipIt.Click, AddressOf Me.BtnZipItClick
			'
			'MainForm
			'
			Me.AutoScaleBaseSize = New System.Drawing.Size(5, 14)
			Me.ClientSize = New System.Drawing.Size(552, 86)
			Me.Controls.Add(Me.label2)
			Me.Controls.Add(Me.label1)
			Me.Controls.Add(Me.txtSourceDir)
			Me.Controls.Add(Me.txtZipFileName)
			Me.Controls.Add(Me.btnBrowseForFolder)
			Me.Controls.Add(Me.btnZipIt)
			Me.Name = "MainForm"
			Me.Text = "MainForm"
			Me.ResumeLayout(false)
		End Sub
		#End Region
		
		Private Sub BtnBrowseForFolderClick(sender As System.Object, e As System.EventArgs)
			If folderBrowserDialog.ShowDialog() = Windows.Forms.DialogResult.OK Then
				txtSourceDir.Text = folderBrowserDialog.SelectedPath
			End If
		End Sub
		
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
		
	End Class
End Namespace

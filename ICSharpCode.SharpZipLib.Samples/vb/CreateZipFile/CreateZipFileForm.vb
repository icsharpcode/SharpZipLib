Imports System
Imports System.IO
Imports System.Drawing
Imports System.Windows.Forms

Imports ICSharpCode.SharpZipLib.Core
Imports ICSharpCode.SharpZipLib.Zip

Namespace CreateZipFile

	Public Class CreateZipFileForm
		Inherits System.Windows.Forms.Form
        Private label2 As System.Windows.Forms.Label
		Private label1 As System.Windows.Forms.Label
		Private txtSourceDir As System.Windows.Forms.TextBox
        Private folderBrowserDialog As System.Windows.Forms.FolderBrowserDialog
        Friend WithEvents btnBrowseForFolder As System.Windows.Forms.Button
        Friend WithEvents btnZipIt As System.Windows.Forms.Button
        Private txtZipFileName As System.Windows.Forms.TextBox

        Public Shared Sub Main()
			Dim fMainForm As New CreateZipFileForm
			fMainForm.ShowDialog()
        End Sub

        Public Sub New()
            MyBase.New()
            '
            ' The Me.InitializeComponent call is required for Windows Forms designer support.
            '
            Me.InitializeComponent()
        End Sub

#Region " Windows Forms Designer generated code "
        ' This method is required for Windows Forms designer support.
        ' Do not change the method contents inside the source code editor. The Forms designer might
        ' not be able to load this method if it was changed manually.
        Private Sub InitializeComponent()
            Me.txtZipFileName = New System.Windows.Forms.TextBox
            Me.folderBrowserDialog = New System.Windows.Forms.FolderBrowserDialog
            Me.txtSourceDir = New System.Windows.Forms.TextBox
            Me.label1 = New System.Windows.Forms.Label
            Me.label2 = New System.Windows.Forms.Label
            Me.btnBrowseForFolder = New System.Windows.Forms.Button
            Me.btnZipIt = New System.Windows.Forms.Button
            Me.SuspendLayout()
            '
            'txtZipFileName
            '
            Me.txtZipFileName.Location = New System.Drawing.Point(16, 37)
            Me.txtZipFileName.Name = "txtZipFileName"
            Me.txtZipFileName.Size = New System.Drawing.Size(100, 20)
            Me.txtZipFileName.TabIndex = 2
            Me.txtZipFileName.Text = "Demo.Zip"
            '
            'txtSourceDir
            '
            Me.txtSourceDir.Location = New System.Drawing.Point(144, 37)
            Me.txtSourceDir.Name = "txtSourceDir"
            Me.txtSourceDir.Size = New System.Drawing.Size(256, 20)
            Me.txtSourceDir.TabIndex = 3
            '
            'label1
            '
            Me.label1.Location = New System.Drawing.Point(144, 7)
            Me.label1.Name = "label1"
            Me.label1.Size = New System.Drawing.Size(224, 22)
            Me.label1.TabIndex = 1
            Me.label1.Text = "Source directory:"
            Me.label1.TextAlign = System.Drawing.ContentAlignment.BottomLeft
            '
            'label2
            '
            Me.label2.Location = New System.Drawing.Point(16, 7)
            Me.label2.Name = "label2"
            Me.label2.Size = New System.Drawing.Size(100, 22)
            Me.label2.TabIndex = 0
            Me.label2.Text = "Zip File Name:"
            Me.label2.TextAlign = System.Drawing.ContentAlignment.BottomLeft
            '
            'btnBrowseForFolder
            '
            Me.btnBrowseForFolder.Location = New System.Drawing.Point(406, 37)
            Me.btnBrowseForFolder.Name = "btnBrowseForFolder"
            Me.btnBrowseForFolder.Size = New System.Drawing.Size(32, 21)
            Me.btnBrowseForFolder.TabIndex = 6
            Me.btnBrowseForFolder.Text = "..."
            Me.btnBrowseForFolder.UseVisualStyleBackColor = True
            '
            'btnZipIt
            '
            Me.btnZipIt.Location = New System.Drawing.Point(455, 37)
            Me.btnZipIt.Name = "btnZipIt"
            Me.btnZipIt.Size = New System.Drawing.Size(75, 20)
            Me.btnZipIt.TabIndex = 7
            Me.btnZipIt.Text = "Zip It"
            Me.btnZipIt.UseVisualStyleBackColor = True
            '
            'MainForm
            '
            Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
            Me.ClientSize = New System.Drawing.Size(552, 86)
            Me.Controls.Add(Me.btnZipIt)
            Me.Controls.Add(Me.btnBrowseForFolder)
            Me.Controls.Add(Me.label2)
            Me.Controls.Add(Me.label1)
            Me.Controls.Add(Me.txtSourceDir)
            Me.Controls.Add(Me.txtZipFileName)
            Me.Name = "MainForm"
            Me.Text = "Create Zip File Sample"
            Me.ResumeLayout(False)
            Me.PerformLayout()

        End Sub
#End Region

        Private Sub BtnBrowseForFolderClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnBrowseForFolder.Click
            If folderBrowserDialog.ShowDialog() = Windows.Forms.DialogResult.OK Then
                txtSourceDir.Text = folderBrowserDialog.SelectedPath
            End If
        End Sub

        Private Sub BtnZipItClick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnZipIt.Click
            Dim sourceDir As String = txtSourceDir.Text.Trim()

            ' Simple sanity checks
            If sourceDir.Length = 0 Then
                MessageBox.Show("Please specify a directory")
                Return
            Else
                If Not Directory.Exists(sourceDir) Then
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

            Try

                REM Compression Level: 0-9
                REM 0: no(Compression)
                REM 9: maximum compression
                strmZipOutputStream.SetLevel(9)

                Dim strFile As String
                Dim abyBuffer(4096) As Byte

                For Each strFile In astrFileNames
                    Dim strmFile As FileStream = File.OpenRead(strFile)
                    Try

                        Dim objZipEntry As ZipEntry = New ZipEntry(strFile)

                        objZipEntry.DateTime = DateTime.Now
                        objZipEntry.Size = strmFile.Length

                        strmZipOutputStream.PutNextEntry(objZipEntry)
                        StreamUtils.Copy(strmFile, strmZipOutputStream, abyBuffer)
                    Finally
                        strmFile.Close()
                    End Try
                Next

                strmZipOutputStream.Finish()

            Finally
                strmZipOutputStream.Close()
            End Try

            MessageBox.Show("Operation complete")
        End Sub

	End Class
End Namespace

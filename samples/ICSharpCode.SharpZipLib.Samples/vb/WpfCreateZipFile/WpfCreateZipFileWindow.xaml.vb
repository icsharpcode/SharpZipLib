Imports System.IO
Imports ICSharpCode.SharpZipLib.Core
Imports ICSharpCode.SharpZipLib.Zip
Imports WpfFolderBrowser


Class WpfCreateZipFileWindow
    Public Sub New()
        ' This call is required by the designer.
        InitializeComponent()
        ' Add any initialization after the InitializeComponent() call.
    End Sub

	Private Sub btnZipIt_Click(sender As Object, e As RoutedEventArgs) Handles btnZipIt.Click
		Dim sourceDir As String = tbSourceDir.Text.Trim()

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

		Dim targetName As String = tbZipFileName.Text.Trim()
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
			strmZipOutputStream.SetLevel(5)

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

	Private Sub btnBrowseForFolder_Click(sender As Object, e As RoutedEventArgs) Handles btnBrowseForFolder.Click
        Dim FolderBrowserDialog As WpfFolderBrowserDialog
        FolderBrowserDialog = New WpfFolderBrowserDialog
		If FolderBrowserDialog.ShowDialog() = True Then
			tbSourceDir.Text = FolderBrowserDialog.FileName
		End If
	End Sub
End Class

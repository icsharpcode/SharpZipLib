Imports System
Imports System.Windows.Forms
Imports System.IO
Imports ICSharpCode.SharpZipLib.Checksums
Imports ICSharpCode.SharpZipLib.Zip
Imports ICSharpCode.SharpZipLib.GZip

Public Class Form1
	Inherits System.Windows.Forms.Form
	Friend txtZipFileName As System.Windows.Forms.TextBox
	Friend btnZipIt As System.Windows.Forms.Button
	Friend Label2 As System.Windows.Forms.Label
	Friend Label1 As System.Windows.Forms.Label
	Friend txtSourceDir As System.Windows.Forms.TextBox
	

	Public Shared Sub Main()
		Dim fForm1 As New Form1
		fForm1.ShowDialog
	End Sub

	Public Sub New()
		MyBase.New()

		'This call is required by the Windows Form Designer.
		InitializeComponent

		'Add any initialization after the InitializeComponent() call

	End Sub

	Private Sub InitializeComponent()
			Me.txtSourceDir = New System.Windows.Forms.TextBox
			Me.Label1 = New System.Windows.Forms.Label
			Me.Label2 = New System.Windows.Forms.Label
			Me.btnZipIt = New System.Windows.Forms.Button
			Me.txtZipFileName = New System.Windows.Forms.TextBox
			Me.SuspendLayout
			'
			'txtSourceDir
			'
			Me.txtSourceDir.Location = New System.Drawing.Point(200, 16)
			Me.txtSourceDir.Name = "txtSourceDir"
			Me.txtSourceDir.Size = New System.Drawing.Size(216, 22)
			Me.txtSourceDir.TabIndex = 0
			Me.txtSourceDir.Text = ""
			'
			'Label1
			'
			Me.Label1.Location = New System.Drawing.Point(15, 15)
			Me.Label1.Name = "Label1"
			Me.Label1.Size = New System.Drawing.Size(145, 22)
			Me.Label1.TabIndex = 2
			Me.Label1.Text = "Directory to put in zip:"
			'
			'Label2
			'
			Me.Label2.Location = New System.Drawing.Point(16, 40)
			Me.Label2.Name = "Label2"
			Me.Label2.Size = New System.Drawing.Size(120, 22)
			Me.Label2.TabIndex = 4
			Me.Label2.Text = "Name of zip file:"
			'
			'btnZipIt
			'
			Me.btnZipIt.DialogResult = System.Windows.Forms.DialogResult.OK
			Me.btnZipIt.Location = New System.Drawing.Point(448, 16)
			Me.btnZipIt.Name = "btnZipIt"
			Me.btnZipIt.Size = New System.Drawing.Size(69, 22)
			Me.btnZipIt.TabIndex = 2
			Me.btnZipIt.Text = "ZipIt"
			AddHandler Me.btnZipIt.Click, AddressOf Me.BtnZipItClick
			'
			'txtZipFileName
			'
			Me.txtZipFileName.Location = New System.Drawing.Point(200, 40)
			Me.txtZipFileName.Name = "txtZipFileName"
			Me.txtZipFileName.Size = New System.Drawing.Size(216, 22)
			Me.txtZipFileName.TabIndex = 1
			Me.txtZipFileName.Text = ""
			'
			'Form1
			'
			Me.AutoScaleBaseSize = New System.Drawing.Size(6, 15)
			Me.ClientSize = New System.Drawing.Size(528, 104)
			Me.Controls.Add(Me.Label2)
			Me.Controls.Add(Me.txtZipFileName)
			Me.Controls.Add(Me.Label1)
			Me.Controls.Add(Me.btnZipIt)
			Me.Controls.Add(Me.txtSourceDir)
			Me.Name = "Form1"
			Me.Text = "Create Zip File"
			Me.ResumeLayout(false)
		End Sub

	Public Shared Sub ZipFile(ByVal strFileToZip As String, ByVal strZippedFile As String, ByVal nCompressionLevel As Integer, ByVal nBlockSize As Integer)
		If (Not System.IO.File.Exists(strFileToZip)) Then
			Throw New System.IO.FileNotFoundException("The specified file " + strFileToZip + "could not be found. Zipping aborted.")
		End If

		Dim strmStreamToZip As System.IO.FileStream
		strmStreamToZip = New System.IO.FileStream(strFileToZip, System.IO.FileMode.Open, System.IO.FileAccess.Read)

		Dim strmZipFile As System.IO.FileStream
		strmZipFile = System.IO.File.Create(strZippedFile)

		Dim strmZipStream As ZipOutputStream
		strmZipStream = New ZipOutputStream(strmZipFile)

		Dim myZipEntry As ZipEntry
		myZipEntry = New ZipEntry("ZippedFile")
		strmZipStream.PutNextEntry(myZipEntry)
		strmZipStream.SetLevel(nCompressionLevel)

		Dim abyBuffer(nBlockSize) As Byte
		Dim nSize As System.Int32
		nSize = strmStreamToZip.Read(abyBuffer, 0, abyBuffer.Length)
		strmZipStream.Write(abyBuffer, 0, nSize)

		Try
			While (nSize < strmStreamToZip.Length)
				Dim nSizeRead As Integer
				nSizeRead = strmStreamToZip.Read(abyBuffer, 0, abyBuffer.Length)
				strmZipStream.Write(abyBuffer, 0, nSizeRead)
				nSize = nSize + nSizeRead
			End While

		Catch Ex As System.Exception
			Throw Ex

		End Try

		strmZipStream.Finish()
		strmZipStream.Close()
		strmStreamToZip.Close()
	End Sub

Private Sub BtnZipItClick(sender As System.Object, e As System.EventArgs)

	Dim astrFileNames() As String = Directory.GetFiles(txtSourceDir.Text)
	Dim objCrc32 As New Crc32()
	Dim strmZipOutputStream As ZipOutputStream

	strmZipOutputStream = New ZipOutputStream(File.Create(txtZipFileName.Text))
	strmZipOutputStream.SetLevel(6)

	REM Compression Level: 0-9
	REM 0: no(Compression)
	REM 9: maximum compression

	Dim strFile As String

	For Each strFile In astrFileNames
		Dim strmFile As FileStream = File.OpenRead(strFile)
		Dim abyBuffer(strmFile.Length - 1) As Byte

		strmFile.Read(abyBuffer, 0, abyBuffer.Length)
		Dim objZipEntry As ZipEntry = New ZipEntry(strFile)

		objZipEntry.DateTime = DateTime.Now
		objZipEntry.Size = strmFile.Length
		strmFile.Close()
		objCrc32.Reset()
		objCrc32.Update(abyBuffer)
		objZipEntry.Crc = objCrc32.Value
		strmZipOutputStream.PutNextEntry(objZipEntry)
		strmZipOutputStream.Write(abyBuffer, 0, abyBuffer.Length)

	Next

	strmZipOutputStream.Finish()
	strmZipOutputStream.Close()

	MessageBox.Show("Operation complete")

End Sub

End Class


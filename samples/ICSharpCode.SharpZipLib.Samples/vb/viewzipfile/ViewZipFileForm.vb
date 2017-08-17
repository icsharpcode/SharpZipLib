Imports System
Imports System.IO
Imports System.Text
Imports System.Collections
Imports System.Windows.Forms
Imports Microsoft.VisualBasic
Imports ICSharpCode.SharpZipLib.Zip

Public Class ViewZipFileForm
	Inherits System.Windows.Forms.Form
	Friend txtFileName As System.Windows.Forms.TextBox
	Friend chkShowEntry As System.Windows.Forms.CheckBox
	Friend WithEvents btnView As System.Windows.Forms.Button
	Friend Label1 As System.Windows.Forms.Label
	Friend txtContent As System.Windows.Forms.TextBox


	Public Shared Sub Main()
		Dim fMainForm As New ViewZipFileForm
		fMainForm.ShowDialog
	End Sub

	Public Sub New()
		MyBase.New()

		'This call is required by the Windows Form Designer.
		InitializeComponent

		'Add any initialization after the InitializeComponent() call

	End Sub

	Private Sub InitializeComponent()
        Me.txtContent = New System.Windows.Forms.TextBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.btnView = New System.Windows.Forms.Button()
        Me.chkShowEntry = New System.Windows.Forms.CheckBox()
        Me.txtFileName = New System.Windows.Forms.TextBox()
        Me.SuspendLayout
        '
        'txtContent
        '
        Me.txtContent.Location = New System.Drawing.Point(43, 162)
        Me.txtContent.Multiline = true
        Me.txtContent.Name = "txtContent"
        Me.txtContent.Size = New System.Drawing.Size(735, 312)
        Me.txtContent.TabIndex = 5
        '
        'Label1
        '
        Me.Label1.Location = New System.Drawing.Point(43, 27)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(180, 39)
        Me.Label1.TabIndex = 4
        Me.Label1.Text = "Zip File Name:"
        '
        'btnView
        '
        Me.btnView.Location = New System.Drawing.Point(648, 27)
        Me.btnView.Name = "btnView"
        Me.btnView.Size = New System.Drawing.Size(135, 39)
        Me.btnView.TabIndex = 2
        Me.btnView.Text = "View"
        '
        'chkShowEntry
        '
        Me.chkShowEntry.Location = New System.Drawing.Point(43, 95)
        Me.chkShowEntry.Name = "chkShowEntry"
        Me.chkShowEntry.Size = New System.Drawing.Size(187, 40)
        Me.chkShowEntry.TabIndex = 1
        Me.chkShowEntry.Text = "Show File Head"
        '
        'txtFileName
        '
        Me.txtFileName.Location = New System.Drawing.Point(259, 27)
        Me.txtFileName.Name = "txtFileName"
        Me.txtFileName.Size = New System.Drawing.Size(360, 29)
        Me.txtFileName.TabIndex = 0
        '
        'MainForm
        '
        Me.AutoScaleBaseSize = New System.Drawing.Size(9, 22)
        Me.ClientSize = New System.Drawing.Size(832, 554)
        Me.Controls.Add(Me.txtContent)
        Me.Controls.Add(Me.chkShowEntry)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.txtFileName)
        Me.Controls.Add(Me.btnView)
        Me.Name = "MainForm"
        Me.Text = "View Zip file"
        Me.ResumeLayout(false)
        Me.PerformLayout

End Sub

	Private Sub BtnViewClick(sender As System.Object, e As System.EventArgs) Handles btnView.Click

		If txtFileName.Text.Trim().Length = 0
			MessageBox.Show("Please enter a file name", "No file entered")
			Return
		End If
		If Not File.Exists(txtFileName.Text)
			MessageBox.Show(txtFileName.Text, "Cannot open file")
			Return
		End If

		Dim strmZipInputStream As ZipInputStream = New ZipInputStream(File.OpenRead(txtFileName.Text))
		Dim objEntry As ZipEntry
		Dim strBuilder As StringBuilder = New StringBuilder()

		objEntry = strmZipInputStream.GetNextEntry()

		While IsNothing(objEntry) = False
			strBuilder.Append("Name: " + objEntry.Name.ToString + vbCrLf)
			strBuilder.Append("Date: " + objEntry.DateTime.ToString + vbCrLf)
			strBuilder.Append("Size: (-1, if the size information is in the footer)" + vbCrLf)
			strBuilder.Append(vbTab + "Uncompressed: " + objEntry.Size.ToString + vbCrLf)
			strBuilder.Append(vbTab + "Compressed: " + objEntry.CompressedSize.ToString + vbCrLf)

			Dim nSize As Integer = 2048
			Dim abyData(2048) As Byte

			If (True = chkShowEntry.Checked) and objEntry.IsFile Then
				nSize = strmZipInputStream.Read(abyData, 0, abyData.Length)

				If nSize > 0 Then
					strBuilder.Append(New ASCIIEncoding().GetString(abyData, 0, nSize) + vbCrLf)
					strBuilder.Append("---END---" + vbCrLf + vbCrLf)
				End If

			End If

			objEntry = strmZipInputStream.GetNextEntry()
		End While

		txtContent.Text = strBuilder.ToString
		strmZipInputStream.Close()
	End Sub

End Class

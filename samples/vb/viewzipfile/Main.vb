Imports System
Imports System.IO
Imports System.Text
Imports System.Collections
Imports Microsoft.VisualBasic
Imports ICSharpCode.SharpZipLib.Zip
Imports ICSharpCode.SharpZipLib.GZip

Public Class Form1
	Inherits System.Windows.Forms.Form
	Friend txtFileName As System.Windows.Forms.TextBox
	Friend chkShowEntry As System.Windows.Forms.CheckBox
	Friend txtContent As System.Windows.Forms.TextBox
	Friend Label1 As System.Windows.Forms.Label
	Friend btnView As System.Windows.Forms.Button
	

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
			Me.btnView = New System.Windows.Forms.Button
			Me.Label1 = New System.Windows.Forms.Label
			Me.txtContent = New System.Windows.Forms.TextBox
			Me.chkShowEntry = New System.Windows.Forms.CheckBox
			Me.txtFileName = New System.Windows.Forms.TextBox
			Me.SuspendLayout
			'
			'btnView
			'
			Me.btnView.Location = New System.Drawing.Point(360, 16)
			Me.btnView.Name = "btnView"
			Me.btnView.TabIndex = 2
			Me.btnView.Text = "View"
			AddHandler Me.btnView.Click, AddressOf Me.BtnViewClick
			'
			'Label1
			'
			Me.Label1.Location = New System.Drawing.Point(24, 16)
			Me.Label1.Name = "Label1"
			Me.Label1.TabIndex = 4
			Me.Label1.Text = "Zip File Name:"
			'
			'txtContent
			'
			Me.txtContent.Location = New System.Drawing.Point(24, 96)
			Me.txtContent.Multiline = true
			Me.txtContent.Name = "txtContent"
			Me.txtContent.Size = New System.Drawing.Size(408, 184)
			Me.txtContent.TabIndex = 5
			Me.txtContent.Text = ""
			'
			'chkShowEntry
			'
			Me.chkShowEntry.Location = New System.Drawing.Point(24, 56)
			Me.chkShowEntry.Name = "chkShowEntry"
			Me.chkShowEntry.TabIndex = 1
			Me.chkShowEntry.Text = "Show Entry"
			'
			'txtFileName
			'
			Me.txtFileName.Location = New System.Drawing.Point(144, 16)
			Me.txtFileName.Name = "txtFileName"
			Me.txtFileName.Size = New System.Drawing.Size(200, 22)
			Me.txtFileName.TabIndex = 0
			Me.txtFileName.Text = ""
			'
			'Form1
			'
			Me.AutoScaleBaseSize = New System.Drawing.Size(6, 15)
			Me.ClientSize = New System.Drawing.Size(448, 296)
			Me.Controls.Add(Me.txtContent)
			Me.Controls.Add(Me.chkShowEntry)
			Me.Controls.Add(Me.Label1)
			Me.Controls.Add(Me.txtFileName)
			Me.Controls.Add(Me.btnView)
			Me.Name = "Form1"
			Me.Text = "View Zip file"
			Me.ResumeLayout(false)
		End Sub

Private Sub BtnViewClick(sender As System.Object, e As System.EventArgs)

		Dim strmZipInputStream As ZipInputStream = New ZipInputStream(File.OpenRead(txtFileName.Text))
		Dim objEntry As ZipEntry
		Dim strOutput As String
		Dim strBuilder As StringBuilder = New StringBuilder(strOutput)

		objEntry = strmZipInputStream.GetNextEntry()

		While IsNothing(objEntry) = False
			strBuilder.Append("Name: " + objEntry.Name.ToString + vbCrLf)
			strBuilder.Append("Date: " + objEntry.DateTime.ToString + vbCrLf)
			strBuilder.Append("Size: (-1, if the size information is in the footer)" + vbCrLf)
			strBuilder.Append(vbTab + "Uncompressed: " + objEntry.Size.ToString + vbCrLf)
			strBuilder.Append(vbTab + "Compressed: " + objEntry.CompressedSize.ToString + vbCrLf)

			Dim nSize As Integer = 2048
			Dim abyData(2048) As Byte

			If (True = chkShowEntry.Checked) Then
				While True
					nSize = strmZipInputStream.Read(abyData, 0, abyData.Length)

					If nSize > 0 Then
						'    strBuilder.Append(New ASCIIEncoding().GetString(abyData, 0, nSize) + vbCrLf)
					Else
						Exit While
					End If
				End While
			End If

			objEntry = strmZipInputStream.GetNextEntry()
		End While

		txtContent.Text = strBuilder.ToString
		strmZipInputStream.Close()
End Sub

End Class


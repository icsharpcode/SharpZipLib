' SharpZipLibrary samples
' Copyright (c) 2007, AlphaSierraPapa
' All rights reserved.
'
' Redistribution and use in source and binary forms, with or without modification, are
' permitted provided that the following conditions are met:
'
' - Redistributions of source code must retain the above copyright notice, this list
'   of conditions and the following disclaimer.
'
' - Redistributions in binary form must reproduce the above copyright notice, this list
'   of conditions and the following disclaimer in the documentation and/or other materials
'   provided with the distribution.
'
' - Neither the name of the SharpDevelop team nor the names of its contributors may be used to
'   endorse or promote products derived from this software without specific prior written
'   permission.
'
' THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS &AS IS& AND ANY EXPRESS
' OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY
' AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
' CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL
' DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
' DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER
' IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT
' OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

Imports System
Imports System.IO
Imports System.Text
Imports System.Collections
Imports System.Windows.Forms
Imports Microsoft.VisualBasic
Imports ICSharpCode.SharpZipLib.Zip

Public Class MainForm
	Inherits System.Windows.Forms.Form
	Friend txtFileName As System.Windows.Forms.TextBox
	Friend chkShowEntry As System.Windows.Forms.CheckBox
	Friend btnView As System.Windows.Forms.Button
	Friend Label1 As System.Windows.Forms.Label
	Friend txtContent As System.Windows.Forms.TextBox
	

	Public Shared Sub Main()
		Dim fMainForm As New MainForm
		fMainForm.ShowDialog
	End Sub
	
	Public Sub New()
		MyBase.New()

		'This call is required by the Windows Form Designer.
		InitializeComponent

		'Add any initialization after the InitializeComponent() call

	End Sub

	Private Sub InitializeComponent()
			Me.txtContent = New System.Windows.Forms.TextBox
			Me.Label1 = New System.Windows.Forms.Label
			Me.btnView = New System.Windows.Forms.Button
			Me.chkShowEntry = New System.Windows.Forms.CheckBox
			Me.txtFileName = New System.Windows.Forms.TextBox
			Me.SuspendLayout
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
			'Label1
			'
			Me.Label1.Location = New System.Drawing.Point(24, 16)
			Me.Label1.Name = "Label1"
			Me.Label1.TabIndex = 4
			Me.Label1.Text = "Zip File Name:"
			'
			'btnView
			'
			Me.btnView.Location = New System.Drawing.Point(360, 16)
			Me.btnView.Name = "btnView"
			Me.btnView.TabIndex = 2
			Me.btnView.Text = "View"
			AddHandler Me.btnView.Click, AddressOf Me.BtnViewClick
			'
			'chkShowEntry
			'
			Me.chkShowEntry.Location = New System.Drawing.Point(24, 56)
			Me.chkShowEntry.Name = "chkShowEntry"
			Me.chkShowEntry.TabIndex = 1
			Me.chkShowEntry.Text = "Show File Head"
			'
			'txtFileName
			'
			Me.txtFileName.Location = New System.Drawing.Point(144, 16)
			Me.txtFileName.Name = "txtFileName"
			Me.txtFileName.Size = New System.Drawing.Size(200, 20)
			Me.txtFileName.TabIndex = 0
			Me.txtFileName.Text = ""
			'
			'MainForm
			'
			Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
			Me.ClientSize = New System.Drawing.Size(448, 296)
			Me.Controls.Add(Me.txtContent)
			Me.Controls.Add(Me.chkShowEntry)
			Me.Controls.Add(Me.Label1)
			Me.Controls.Add(Me.txtFileName)
			Me.Controls.Add(Me.btnView)
			Me.Name = "MainForm"
			Me.Text = "View Zip file"
			Me.ResumeLayout(false)
		End Sub

	Private Sub BtnViewClick(sender As System.Object, e As System.EventArgs)

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


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
Imports System.Windows.Forms
Imports System.IO

Imports ICSharpCode.SharpZipLib.BZip2

Public Class MainForm
	Inherits System.Windows.Forms.Form
	Friend txtFileName As System.Windows.Forms.TextBox
	Friend btnBrowseForBZ As System.Windows.Forms.Button
	Friend Label1 As System.Windows.Forms.Label
	Friend rdCompress As System.Windows.Forms.RadioButton
	Friend GroupBox1 As System.Windows.Forms.GroupBox
	Friend btnExecute As System.Windows.Forms.Button
	Friend rdDecompress As System.Windows.Forms.RadioButton
	

#Region " Windows Form Designer generated code "

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
			Me.rdDecompress = New System.Windows.Forms.RadioButton
			Me.btnExecute = New System.Windows.Forms.Button
			Me.GroupBox1 = New System.Windows.Forms.GroupBox
			Me.rdCompress = New System.Windows.Forms.RadioButton
			Me.Label1 = New System.Windows.Forms.Label
			Me.btnBrowseForBZ = New System.Windows.Forms.Button
			Me.txtFileName = New System.Windows.Forms.TextBox
			Me.GroupBox1.SuspendLayout
			Me.SuspendLayout
			'
			'rdDecompress
			'
			Me.rdDecompress.Location = New System.Drawing.Point(8, 64)
			Me.rdDecompress.Name = "rdDecompress"
			Me.rdDecompress.TabIndex = 1
			Me.rdDecompress.Text = "decompress"
			'
			'btnExecute
			'
			Me.btnExecute.Location = New System.Drawing.Point(216, 112)
			Me.btnExecute.Name = "btnExecute"
			Me.btnExecute.Size = New System.Drawing.Size(112, 22)
			Me.btnExecute.TabIndex = 2
			Me.btnExecute.Text = "Execute"
			AddHandler Me.btnExecute.Click, AddressOf Me.btnExecuteClick
			'
			'GroupBox1
			'
			Me.GroupBox1.Controls.Add(Me.rdDecompress)
			Me.GroupBox1.Controls.Add(Me.rdCompress)
			Me.GroupBox1.Location = New System.Drawing.Point(8, 40)
			Me.GroupBox1.Name = "GroupBox1"
			Me.GroupBox1.Size = New System.Drawing.Size(160, 96)
			Me.GroupBox1.TabIndex = 3
			Me.GroupBox1.TabStop = false
			Me.GroupBox1.Text = "Operation to perform"
			'
			'rdCompress
			'
			Me.rdCompress.Checked = true
			Me.rdCompress.Location = New System.Drawing.Point(8, 32)
			Me.rdCompress.Name = "rdCompress"
			Me.rdCompress.TabIndex = 0
			Me.rdCompress.TabStop = true
			Me.rdCompress.Text = "compress"
			'
			'Label1
			'
			Me.Label1.Location = New System.Drawing.Point(8, 16)
			Me.Label1.Name = "Label1"
			Me.Label1.Size = New System.Drawing.Size(72, 16)
			Me.Label1.TabIndex = 4
			Me.Label1.Text = "Filename:"
			'
			'btnBrowseForBZ
			'
			Me.btnBrowseForBZ.Location = New System.Drawing.Point(304, 16)
			Me.btnBrowseForBZ.Name = "btnBrowseForBZ"
			Me.btnBrowseForBZ.Size = New System.Drawing.Size(24, 22)
			Me.btnBrowseForBZ.TabIndex = 5
			Me.btnBrowseForBZ.Text = "..."
			AddHandler Me.btnBrowseForBZ.Click, AddressOf Me.btnBrowseForBZClick
			'
			'txtFileName
			'
			Me.txtFileName.Location = New System.Drawing.Point(96, 16)
			Me.txtFileName.Name = "txtFileName"
			Me.txtFileName.Size = New System.Drawing.Size(200, 20)
			Me.txtFileName.TabIndex = 0
			Me.txtFileName.Text = ""
			'
			'MainForm
			'
			Me.AutoScaleBaseSize = New System.Drawing.Size(5, 13)
			Me.ClientSize = New System.Drawing.Size(344, 152)
			Me.Controls.Add(Me.btnBrowseForBZ)
			Me.Controls.Add(Me.Label1)
			Me.Controls.Add(Me.GroupBox1)
			Me.Controls.Add(Me.btnExecute)
			Me.Controls.Add(Me.txtFileName)
			Me.Name = "MainForm"
			Me.Text = "Mini BZ2 Application"
			Me.GroupBox1.ResumeLayout(false)
			Me.ResumeLayout(false)
		End Sub

#End Region

	Private Sub btnExecuteClick(ByVal sender As System.Object, ByVal e As System.EventArgs)
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

			BZip2.Decompress(fsBZ2Archive, fsOutput)

			fsBZ2Archive.Close()
			fsOutput.Flush()
			fsOutput.Close()
		Else
			'Compression of single-file archive
			Dim fsInputFile As FileStream, fsBZ2Archive As FileStream
			fsInputFile = File.OpenRead(txtFileName.Text)
			fsBZ2Archive = File.Create(txtFileName.Text + ".bz")

			BZip2.Compress(fsInputFile, fsBZ2Archive, 4026)

			fsInputFile.Close()
			' fsBZ2Archive.Flush() & fsBZ2Archive.Close() are automatically called by .Compress
		End If
	End Sub

	Private Sub btnBrowseForBZClick(ByVal sender As System.Object, ByVal e As System.EventArgs)
		Dim ofn As New OpenFileDialog()
		
		ofn.InitialDirectory = "c:\"
		ofn.Filter = "BZ files (*.bz)|*.bz|All files (*.*)|*.*"

		If (ofn.ShowDialog() = Windows.Forms.DialogResult.OK) Then
			txtFileName.Text = ofn.FileName
		End If
	End Sub
End Class


﻿Imports System.ComponentModel
Imports System.IO
Imports System.Text

Public Class Nightfire007File
	Inherits BasePackageFile

#Region "Creation and Destruction"

	Public Sub New(ByVal nightfire007FileReader As BinaryReader, ByVal nightfire007FileData As Nightfire007FileData)
		Me.theInputFileReader = nightfire007FileReader
		Me.theNightfire007FileData = nightfire007FileData
	End Sub

#End Region

#Region "Properties"

	Public ReadOnly Property FileData() As Nightfire007FileData
		Get
			Return Me.theNightfire007FileData
		End Get
	End Property

#End Region

#Region "Methods"

	'       Magic: Integer;   // Always 1 ?
	'       Version: Integer; // 1 = Used in Nightfire Demo
	'                         // 3 = Used in Nightfire Retail
	'       Magic2: Integer;
	'       ID: array[0..3] of char;
	'       NumRootDirs: Integer;
	'     end;
	'     // If version = 3 then following the header is an integer giving the number of entries
	'     // Get32 filename
	'     N007Entry = packed record
	'       Compressed: byte;
	'       Size: integer;
	'       CompSize: integer;
	'     end;
	'     // If version = 1 then follows Size bytes of data (if Compressed = 0) or CompSize (if Compressed = 1)
	Public Overrides Sub ReadHeader()
		'Dim inputFileStreamPosition As Long
		Dim fileOffsetStart As Long
		Dim fileOffsetEnd As Long
		'Dim fileOffsetStart2 As Long
		'Dim fileOffsetEnd2 As Long

		fileOffsetStart = Me.theInputFileReader.BaseStream.Position

		Me.theNightfire007FileData.id(0) = Me.theInputFileReader.ReadUInt32()
		Me.theNightfire007FileData.id(1) = Me.theInputFileReader.ReadUInt32()
		Me.theNightfire007FileData.id(2) = Me.theInputFileReader.ReadUInt32()
		Me.theNightfire007FileData.id(3) = Me.theInputFileReader.ReadUInt32()

		fileOffsetEnd = Me.theInputFileReader.BaseStream.Position - 1
		Me.theNightfire007FileData.theFileSeekLog.Add(fileOffsetStart, fileOffsetEnd, "Nightfire 007 File Header")
	End Sub

	Private Sub ReadValveVpkHeader()
		Me.theNightfire007FileData.version = Me.theInputFileReader.ReadUInt32()
		Me.theNightfire007FileData.directoryLength = Me.theInputFileReader.ReadUInt32()

		If Me.theNightfire007FileData.version = 2 Then
			Me.theNightfire007FileData.unused01 = Me.theInputFileReader.ReadUInt32()
			Me.theNightfire007FileData.archiveHashLength = Me.theInputFileReader.ReadUInt32()
			Me.theNightfire007FileData.extraLength = Me.theInputFileReader.ReadUInt32()
			Me.theNightfire007FileData.unused01 = Me.theInputFileReader.ReadUInt32()
		ElseIf Me.theNightfire007FileData.version = 196610 Then
			Me.theNightfire007FileData.unused01 = Me.theInputFileReader.ReadUInt32()
		End If

		Me.theNightfire007FileData.theDirectoryOffset = Me.theInputFileReader.BaseStream.Position
	End Sub

	Private Function IsVtmbVpk() As Boolean
		Dim theVpkIsVtmb As Boolean = False

		Me.theInputFileReader.BaseStream.Seek(-1, SeekOrigin.End)
		Dim vtmbVpkType As Integer = Me.theInputFileReader.ReadByte()
		'NOTE: Skip reading vtmbVpkType = 1 because it is just a directory of entries with no data.
		If vtmbVpkType = 0 OrElse vtmbVpkType = 1 Then
			Dim directoryEndOffset As Long = Me.theInputFileReader.BaseStream.Seek(-9, SeekOrigin.End)
			Me.theNightfire007FileData.theEntryCount = Me.theInputFileReader.ReadUInt32()
			Me.theNightfire007FileData.theDirectoryOffset = Me.theInputFileReader.ReadUInt32()
			'TODO: It is VTMB VPK package if offsets and lengths match in the directory at end of file.
			'      Would need to check that offsets and lengths are within file length boundaries.
			theVpkIsVtmb = True
			Dim entryPathFileNameLength As UInteger
			Try
				Me.theInputFileReader.BaseStream.Seek(Me.theNightfire007FileData.theDirectoryOffset, SeekOrigin.Begin)
				For i As UInteger = 0 To CUInt(Me.theNightfire007FileData.theEntryCount - 1)
					entryPathFileNameLength = Me.theInputFileReader.ReadUInt32()
					'entry.thePathFileName = Me.theInputFileReader.ReadChars(CInt(entryPathFileNameLength))
					'entry.dataOffset = Me.theInputFileReader.ReadUInt32()
					'entry.dataLength = Me.theInputFileReader.ReadUInt32()
					Me.theInputFileReader.BaseStream.Seek(entryPathFileNameLength + 8, SeekOrigin.Current)
				Next
				'NOTE: Do not accept 'vtmbVpkType = 1' as a valid VtmbVpk because it is just a directory of entries with no data.
				If Me.theInputFileReader.BaseStream.Position <> directoryEndOffset OrElse vtmbVpkType = 1 Then
					Me.theNightfire007FileData.theEntryCount = 0
					theVpkIsVtmb = False
				End If
			Catch ex As Exception
				Me.theNightfire007FileData.theEntryCount = 0
				theVpkIsVtmb = False
			End Try
		End If

		Return theVpkIsVtmb
	End Function

	'Example output:
	'addonimage.jpg crc=0x50ea4a15 metadatasz=0 fnumber=32767 ofs=0x0 sz=10749
	'addonimage.vtf crc=0xc75861f5 metadatasz=0 fnumber=32767 ofs=0x29fd sz=8400
	'addoninfo.txt crc=0xb3d2b571 metadatasz=0 fnumber=32767 ofs=0x4acd sz=1677
	'materials/models/weapons/melee/crowbar.vmt crc=0x4aaf5f0 metadatasz=0 fnumber=32767 ofs=0x515a sz=566
	'materials/models/weapons/melee/crowbar.vtf crc=0xded2e058 metadatasz=0 fnumber=32767 ofs=0x5390 sz=174920
	'materials/models/weapons/melee/crowbar_normal.vtf crc=0x7ac0e054 metadatasz=0 fnumber=32767 ofs=0x2fed8 sz=1398196
	Public Overrides Sub ReadEntries(ByVal bw As BackgroundWorker)
		'Dim inputFileStreamPosition As Long
		'Dim fileOffsetStart As Long
		'Dim fileOffsetEnd As Long
		'Dim fileOffsetStart2 As Long
		'Dim fileOffsetEnd2 As Long

		'fileOffsetStart = Me.theInputFileReader.BaseStream.Position

		'If Me.theVpkFileData.id <> Nightfire007FileData.VPK_ID OrElse Me.theVpkFileData.id <> Nightfire007FileData.FPX_ID Then
		'	Exit Sub
		'End If
		If Not Me.theNightfire007FileData.IsSourcePackage Then
			Exit Sub
		End If

		If Not Me.theNightfire007FileData.PackageHasID Then
			ReadVtmbEntries(bw)
			Exit Sub
		End If

		Dim vpkFileHasMoreToRead As Boolean = True
		Dim entryExtension As String = ""
		Dim entryPath As String = ""
		Dim entryFileName As String = ""
		Dim entry As VpkDirectoryEntry
		Dim entryDataOutputText As New StringBuilder
		While vpkFileHasMoreToRead
			Try
				entryExtension = FileManager.ReadNullTerminatedString(Me.theInputFileReader)
				If String.IsNullOrEmpty(entryExtension) Then
					Exit While
				End If
				If bw IsNot Nothing AndAlso bw.CancellationPending Then
					vpkFileHasMoreToRead = False
				End If
			Catch ex As Exception
				'vpkFileHasMoreToRead = False
				Exit While
			End Try

			While vpkFileHasMoreToRead
				Try
					entryPath = FileManager.ReadNullTerminatedString(Me.theInputFileReader)
					If String.IsNullOrEmpty(entryPath) Then
						Exit While
					End If
					If bw IsNot Nothing AndAlso bw.CancellationPending Then
						vpkFileHasMoreToRead = False
					End If
				Catch ex As Exception
					vpkFileHasMoreToRead = False
					Exit While
				End Try

				While vpkFileHasMoreToRead
					Try
						entryFileName = FileManager.ReadNullTerminatedString(Me.theInputFileReader)
						If String.IsNullOrEmpty(entryFileName) Then
							Exit While
						End If
						If bw IsNot Nothing AndAlso bw.CancellationPending Then
							vpkFileHasMoreToRead = False
						End If
					Catch ex As Exception
						vpkFileHasMoreToRead = False
						Exit While
					End Try

					entry = New VpkDirectoryEntry()
					entry.crc = Me.theInputFileReader.ReadUInt32()
					entry.preloadByteCount = Me.theInputFileReader.ReadUInt16()
					entry.archiveIndex = Me.theInputFileReader.ReadUInt16()
					If Me.theNightfire007FileData.version = 196610 Then
						'TODO: Exit for now so Crowbar does not freeze.
						Exit Sub
						'' 01 01
						'entry.unknown01 = Me.theInputFileReader.ReadUInt16()
						'' 00 00 00 80 
						'entry.unknown02 = Me.theInputFileReader.ReadUInt32()
						'entry.dataOffset = Me.theInputFileReader.ReadUInt32()
						'entry.unknown03 = Me.theInputFileReader.ReadUInt32()
						'entry.dataLength = Me.theInputFileReader.ReadUInt32()
						'entry.unknown04 = Me.theInputFileReader.ReadUInt32()
						'entry.fileSize = Me.theInputFileReader.ReadUInt32()
						'entry.unknown05 = Me.theInputFileReader.ReadUInt32()
						'' FF FF
						'entry.endOfEntryBytes = Me.theInputFileReader.ReadUInt16()
					Else
						entry.dataOffset = Me.theInputFileReader.ReadUInt32()
						entry.dataLength = Me.theInputFileReader.ReadUInt32()
						entry.endBytes = Me.theInputFileReader.ReadUInt16()

						If entry.preloadByteCount > 0 Then
							entry.preloadBytesOffset = Me.theInputFileReader.BaseStream.Position
							Me.theInputFileReader.ReadBytes(entry.preloadByteCount)
						End If
					End If

					If entryPath = " " Then
						entry.thePathFileName = entryFileName + "." + entryExtension
					Else
						entry.thePathFileName = entryPath + "/" + entryFileName + "." + entryExtension
					End If
					Me.theNightfire007FileData.theEntries.Add(entry)

					entryDataOutputText.Append(entry.thePathFileName)
					entryDataOutputText.Append(" crc=0x" + entry.crc.ToString("X8"))
					entryDataOutputText.Append(" metadatasz=" + entry.preloadByteCount.ToString("G0"))
					entryDataOutputText.Append(" fnumber=" + entry.archiveIndex.ToString("G0"))
					entryDataOutputText.Append(" ofs=0x" + entry.dataOffset.ToString("X8"))
					entryDataOutputText.Append(" sz=" + (entry.preloadByteCount + entry.dataLength).ToString("G0"))

					Me.theNightfire007FileData.theEntryDataOutputTexts.Add(entryDataOutputText.ToString())
					NotifyPackEntryRead(entry, entryDataOutputText.ToString())

					entryDataOutputText.Clear()

					If bw IsNot Nothing AndAlso bw.CancellationPending Then
						vpkFileHasMoreToRead = False
					End If
				End While
			End While
		End While

		'fileOffsetEnd = Me.theInputFileReader.BaseStream.Position - 1
		'Me.theVpkFileData.theFileSeekLog.Add(fileOffsetStart, fileOffsetEnd, "VPK File Header")
	End Sub

	Private Sub ReadVtmbEntries(ByVal bw As BackgroundWorker)
		Dim entryPathFileNameLength As UInteger
		Dim entryFileName As String = ""
		Dim entry As VpkDirectoryEntry
		Dim entryDataOutputText As New StringBuilder

		Me.theInputFileReader.BaseStream.Seek(Me.theNightfire007FileData.theDirectoryOffset, SeekOrigin.Begin)
		For i As UInteger = 0 To CUInt(Me.theNightfire007FileData.theEntryCount - 1)
			entry = New VpkDirectoryEntry()

			entryPathFileNameLength = Me.theInputFileReader.ReadUInt32()
			entry.thePathFileName = Me.theInputFileReader.ReadChars(CInt(entryPathFileNameLength))
			entry.dataOffset = Me.theInputFileReader.ReadUInt32()
			entry.dataLength = Me.theInputFileReader.ReadUInt32()

			entry.crc = 0
			entry.preloadByteCount = 0
			'entry.archiveIndex = &H7FFF
			entry.endBytes = 0
			entry.isVtmbVpk = True

			Me.theNightfire007FileData.theEntries.Add(entry)

			entryDataOutputText.Append(entry.thePathFileName)
			entryDataOutputText.Append(" crc=0x" + entry.crc.ToString("X8"))
			entryDataOutputText.Append(" metadatasz=" + entry.preloadByteCount.ToString("G0"))
			entryDataOutputText.Append(" fnumber=" + entry.archiveIndex.ToString("G0"))
			entryDataOutputText.Append(" ofs=0x" + entry.dataOffset.ToString("X8"))
			entryDataOutputText.Append(" sz=" + (entry.preloadByteCount + entry.dataLength).ToString("G0"))

			Me.theNightfire007FileData.theEntryDataOutputTexts.Add(entryDataOutputText.ToString())
			NotifyPackEntryRead(entry, entryDataOutputText.ToString())

			entryDataOutputText.Clear()

			If bw IsNot Nothing AndAlso bw.CancellationPending Then
				Exit For
			End If
		Next
	End Sub

	Public Overrides Sub UnpackEntryDataToFile(ByVal iEntry As BasePackageDirectoryEntry, ByVal outputPathFileName As String)
		Dim entry As VpkDirectoryEntry
		entry = CType(iEntry, VpkDirectoryEntry)

		Dim outputFileStream As FileStream = Nothing
		Try
			outputFileStream = New FileStream(outputPathFileName, FileMode.Create)
			If outputFileStream IsNot Nothing Then
				Try
					Me.theOutputFileWriter = New BinaryWriter(outputFileStream, System.Text.Encoding.ASCII)

					If entry.archiveIndex = &H7FFF AndAlso Not entry.isVtmbVpk Then
						Me.theInputFileReader.BaseStream.Seek(Me.theNightfire007FileData.theDirectoryOffset + Me.theNightfire007FileData.directoryLength + entry.dataOffset, SeekOrigin.Begin)
					Else
						Me.theInputFileReader.BaseStream.Seek(entry.dataOffset, SeekOrigin.Begin)
					End If
					Dim bytes() As Byte
					bytes = Me.theInputFileReader.ReadBytes(CInt(entry.dataLength))
					Me.theOutputFileWriter.Write(bytes)
				Catch ex As Exception
					Dim debug As Integer = 4242
				Finally
					If Me.theOutputFileWriter IsNot Nothing Then
						Me.theOutputFileWriter.Close()
					End If
				End Try
			End If
		Catch ex As Exception
			Dim debug As Integer = 4242
		Finally
			If outputFileStream IsNot Nothing Then
				outputFileStream.Close()
			End If
		End Try
	End Sub

#End Region

#Region "Private Methods"

#End Region

#Region "Data"

	Private theInputFileReader As BinaryReader
	Private theOutputFileWriter As BinaryWriter
	Private theNightfire007FileData As Nightfire007FileData

#End Region

End Class

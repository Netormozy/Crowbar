﻿Imports System.IO

Public Class SourceAniFile49
	Inherits SourceMdlFile49

#Region "Creation and Destruction"

	Public Sub New(ByVal aniFileReader As BinaryReader, ByVal aniFileData As SourceFileData, ByVal mdlFileData As SourceFileData)
		Me.theInputFileReader = aniFileReader
		Me.theMdlFileData = CType(aniFileData, SourceMdlFileData49)
		Me.theRealMdlFileData = CType(mdlFileData, SourceMdlFileData49)

		Me.theMdlFileData.theFileSeekLog.FileSize = Me.theInputFileReader.BaseStream.Length

		'NOTE: Need the bone data from the real MDL file because SourceAniFile inherits SourceMdlFile.ReadMdlAnimation() that uses the data.
		Me.theMdlFileData.theBones = Me.theRealMdlFileData.theBones
	End Sub

#End Region

#Region "Methods"

	'TODO: [2015-08-16] Currently the same as SourceAniFile48. Not sure how to share the code while still having the two versions call different ReadAniAnimation().
	'Public Sub ReadAniBlocks(ByVal delegateReadAniAnimation As ReadAniAnimationDelegate)
	Public Sub ReadAnimationAniBlocks()
		If Me.theRealMdlFileData.theAnimationDescs IsNot Nothing Then
			Dim animBlockInputFileStreamPosition As Long
			'Dim animBlockInputFileStreamEndPosition As Long
			Dim anAnimationDesc As SourceMdlAnimationDesc49

			For anAnimDescIndex As Integer = 0 To Me.theRealMdlFileData.theAnimationDescs.Count - 1
				anAnimationDesc = Me.theRealMdlFileData.theAnimationDescs(anAnimDescIndex)

				animBlockInputFileStreamPosition = Me.theRealMdlFileData.theAnimBlocks(anAnimationDesc.animBlock).dataStart
				'animBlockInputFileStreamEndPosition = Me.theRealMdlFileData.theAnimBlocks(anAnimationDesc.animBlock).dataEnd

				Try
					Dim sectionIndex As Integer
					If anAnimationDesc.theSections IsNot Nothing AndAlso anAnimationDesc.theSections.Count > 0 Then
						Dim sectionFrameCount As Integer
						Dim sectionCount As Integer

						sectionCount = anAnimationDesc.theSections.Count
						For sectionIndex = 0 To sectionCount - 1
							If anAnimationDesc.theSections(sectionIndex).animBlock > 0 Then
								If sectionIndex < sectionCount - 2 Then
									sectionFrameCount = anAnimationDesc.sectionFrameCount
								Else
									'NOTE: Due to the weird calculation of sectionCount in studiomdl, this line is called twice, which means there are two "last" sections.
									'      This also likely means that the last section is bogus unused data.
									sectionFrameCount = anAnimationDesc.frameCount - ((sectionCount - 2) * anAnimationDesc.sectionFrameCount)
								End If

								animBlockInputFileStreamPosition = Me.theRealMdlFileData.theAnimBlocks(anAnimationDesc.theSections(sectionIndex).animBlock).dataStart
								'animBlockInputFileStreamEndPosition = Me.theRealMdlFileData.theAnimBlocks(anAnimationDesc.theSections(sectionIndex).animBlock).dataEnd
								Me.ReadAniAnimation(animBlockInputFileStreamPosition + anAnimationDesc.theSections(sectionIndex).animOffset, anAnimationDesc, sectionFrameCount, sectionIndex, (sectionIndex >= sectionCount - 2) Or (anAnimationDesc.frameCount = (sectionIndex + 1) * anAnimationDesc.sectionFrameCount))
							End If
						Next
					ElseIf anAnimationDesc.animBlock > 0 Then
						sectionIndex = 0
						Me.ReadAniAnimation(animBlockInputFileStreamPosition + anAnimationDesc.animOffset, anAnimationDesc, anAnimationDesc.frameCount, sectionIndex, True)
					End If

					If anAnimationDesc.animBlock > 0 Then
						Me.ReadMdlIkRules(animBlockInputFileStreamPosition + anAnimationDesc.animblockIkRuleOffset, anAnimationDesc)
						Me.ReadLocalHierarchies(animBlockInputFileStreamPosition, anAnimationDesc)
					End If
				Catch ex As Exception
					Dim debug As Integer = 4242
				End Try
			Next
		End If
	End Sub

#End Region

#Region "Private Methods"

	Private Sub ReadAniAnimation(ByVal aniFileInputFileStreamPosition As Long, ByVal anAnimationDesc As SourceMdlAnimationDesc49, ByVal sectionFrameCount As Integer, ByVal sectionIndex As Integer, ByVal lastSectionIsBeingRead As Boolean)
		Me.ReadAnimationFrameByBone(aniFileInputFileStreamPosition, anAnimationDesc, sectionFrameCount, sectionIndex, lastSectionIsBeingRead)
	End Sub

#End Region

#Region "Data"

	Protected theRealMdlFileData As SourceMdlFileData49

#End Region

End Class

Imports System.Collections.Specialized

Public Class QUEUECONTROL
    Private ReadOnly QUEUE As New StringCollection


    Public Sub ADDTOQUEUE(URL As String)

        QUEUE.Add(URL)
    End Sub

    Public Function GETFULLQUEUE() As StringCollection
        Return QUEUE
    End Function

    Public Sub DELFROMQUEUE(URL As String)

        QUEUE.Remove(URL)
    End Sub

    Public Function GETNEXTQUEUE() As String

        Return QUEUE(0).ToString()
    End Function

    Public Function CHECKINSIDEQUEUE(URL As String) As Boolean

        If QUEUE.Contains(URL) Then
            Return True
        Else
            Return False
        End If
    End Function

    Public Function GETNUMBER() As Integer
        Return QUEUE.Count
    End Function
End Class

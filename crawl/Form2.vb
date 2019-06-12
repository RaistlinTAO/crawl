Imports System.Collections.Specialized
Imports System.Xml
Imports System.IO
Imports System.Text.RegularExpressions

Public Class Form2
    ReadOnly MOBILEURL As New StringCollection

    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ListBox1.Items.Clear()
        TextBox1.Text = ""
        MOBILEURL.Clear()
        Dim doc As New XmlDocument
        doc.Load(Application.StartupPath + "\Classification.xml")
        Dim list = doc.GetElementsByTagName("MOBILEURL")
        For Each item As XmlElement In list
            MOBILEURL.Add(item.InnerText)
        Next

        SHOWXMLFILE()
    End Sub

    Private Sub SHOWXMLFILE()

        ListBox1.Items.Clear()

        For Each o As String In MOBILEURL
            ListBox1.Items.Add(o)
        Next
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        'USE MOBILE URL TO EDIT THE XML FILE

        File.Delete(Application.StartupPath + "\Classification.xml")


        Dim textstring As String = "<CLASSIFICATIONMODEL>" + vbNewLine

        For Each s As String In MOBILEURL

            textstring = textstring + "<MOBILEURL>" + s + "</MOBILEURL>" + vbNewLine

        Next

        textstring = textstring + "</CLASSIFICATIONMODEL>"

        File.WriteAllText(Application.StartupPath + "\Classification.xml", textstring)

        DialogResult = DialogResult.OK
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        If ListBox1.SelectedIndex >= 0 Then

            MOBILEURL.RemoveAt(ListBox1.SelectedIndex)
            SHOWXMLFILE()
        Else
            Return

        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        If TextBox1.Text <> "" And CHECKURL(TextBox1.Text) Then
            MOBILEURL.Add(TextBox1.Text)
            SHOWXMLFILE()
        Else
            Return
        End If
    End Sub


    Private Function CHECKURL(URL As String) As Boolean
        Try
            Dim _
                REGEX As _
                    New Regex("\b(https?|ftp|file)://[-A-Z0-9+&@#/%?=~_|$!:,.;]*[A-Z0-9+&@#/%=~_|$]",
                              RegexOptions.IgnoreCase)
            Return REGEX.IsMatch(URL)

        Catch ex As ArgumentException
            'Syntax error in the regular expression
            Return False
        End Try
    End Function
End Class
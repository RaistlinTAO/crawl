Imports System.Collections.Specialized
Imports System.Xml
Imports System.Threading
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Public Class form1
    Declare Function SendMessage Lib "user32" Alias "SendMessageA"(
                                                                   ByVal hwnd As IntPtr,
                                                                   ByVal wMsg As Integer,
                                                                   ByVal wParam As Integer,
                                                                   ByVal lParam As Integer) _
        As Boolean
    Declare Function ReleaseCapture Lib "user32" Alias "ReleaseCapture"() As Boolean
    Const WM_SYSCOMMAND = &H112
    Const SC_MOVE = &HF010&
    Const HTCAPTION = 2

    Dim POLITENESS As Integer = 10
    Dim MAXPAGES As Integer = 20
    Dim SEEDURL As String
    ReadOnly URLPOOL As New QUEUECONTROL


    ReadOnly URLVISITEDMOBILEPOOL As New QUEUECONTROL
    ReadOnly URLVISITEDNONMOBILEPOOL As New QUEUECONTROL
    ReadOnly MOBILEPAGESIZE As New StringCollection
    ReadOnly NONMOBILEPAGESIZE As New StringCollection
    ReadOnly MOBILEPAGEENCODE As New StringCollection
    ReadOnly NONMOBILEPAGEENCODE As New StringCollection

    Dim URLHOST As String
    Dim TICKET As Boolean

    Dim FULLLOAD As Boolean
    ReadOnly MOBILEURL As New StringCollection


    Private Sub cmdExit_Click(sender As Object, e As EventArgs) Handles cmdExit.Click
        WebBrowser1.Stop()
        Application.Exit()
    End Sub

    Private Sub form1_MouseMove(sender As Object, e As MouseEventArgs) Handles MyBase.MouseMove
        ReleaseCapture()
        SendMessage(Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0)
    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        WindowState = FormWindowState.Minimized
    End Sub

    Private Sub form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim CMD = Command()
        Dim TEMP = CMD.Split(" ")
        For i As Integer = 0 To TEMP.Length - 1
            If TEMP(i) = ("-politeness") Then
                POLITENESS = Integer.Parse(TEMP(i + 1))
                LIST1.Items.Add("POLITENESS = " + POLITENESS.ToString())
            End If
            If TEMP(i) = ("-maxpages") Then
                MAXPAGES = Integer.Parse(TEMP(i + 1))
                LIST1.Items.Add("MAXPAGES = " + MAXPAGES.ToString())
            End If
        Next
        If CHECKURL(TEMP(TEMP.Length - 1)) Then
            SEEDURL = TEMP(TEMP.Length - 1)
            LIST1.Items.Add("SEEDURL = " + SEEDURL)
        Else
            MessageBox.Show("SEED URL ERROR!")
            Application.Exit()
        End If

        URLHOST = GETURLHOST(SEEDURL)

        URLPOOL.ADDTOQUEUE(SEEDURL)
    End Sub

    Private Sub STARTJOB()

        Dim doc As New XmlDocument
        doc.Load(Application.StartupPath + "\Classification.xml")
        Dim list = doc.GetElementsByTagName("MOBILEURL")
        For Each item As XmlElement In list
            MOBILEURL.Add(item.InnerText)
        Next

        ProgressBar1.Value = 0
        ProgressBar1.Maximum = MAXPAGES

        For i As Integer = 0 To MAXPAGES - 1

            ProgressBar1.Value = i + 1
            If (URLPOOL.GETNUMBER() > 0) Then
                Dim TEMPURL As String = URLPOOL.GETNEXTQUEUE()
                LIST1.Items.Add("START FETCH " + TEMPURL)
                DOWNLOADURL(TEMPURL, i)
                URLPOOL.DELFROMQUEUE(TEMPURL)
                'URLVISITEDPOOL.ADDTOQUEUE(TEMPURL)
            Else
                MessageBox.Show("JOB FINISHED")
                Return
            End If
            Thread.Sleep(POLITENESS*1000)
        Next
    End Sub

    Private Sub DOWNLOADURL(URL As String, INDEX As Integer)
        FULLLOAD = False
        WebBrowser1.Navigate(URL)
        TICKET = False
        Timer1.Start()
        Do While FULLLOAD = False
            If (TICKET) Then
                LIST1.Items.Add("NETWORK ERROR, FAIL TO LOAD: " + URL + "  TRY NEXT")
                WebBrowser1.Stop()
                Return
            End If
            Application.DoEvents()
        Loop
        'CHECK URL INSIDE
        Try
            Dim doc As HtmlDocument = WebBrowser1.Document
            Dim allHyperlinks As HtmlElementCollection = doc.Links ' doc.GetElementsByTagName("A")
            Dim hyperlink As HtmlElement = Nothing
            Dim href As String = String.Empty

            Dim j As Integer = 0
            For i = 0 To allHyperlinks.Count - 1
                hyperlink = allHyperlinks(i)
                href = hyperlink.GetAttribute("href")

                'BEFORE ADD THE URL TO THE QUEUE , CHECK URL VALID OR NOT
                If CHECKURLVALID(href) Then
                    URLPOOL.ADDTOQUEUE(href)
                    j = j + 1
                End If

            Next

            LIST1.Items.Add("FOUND " + j.ToString() + " URLS AND ADDED TO QUEUE")

            Dim sr As New StreamReader(WebBrowser1.DocumentStream, Encoding.GetEncoding(doc.Encoding))
            Dim HTML As String = sr.ReadToEnd()

            'CHECK CLASSIFICATION

            'IF IT IS MOBILE PUT IT INTO URLVISITEDMOBILEPOOL, OTHERWISE PUT INTO URLVISITEDNONMOBILEPOOL
            'USE XML AS CLASSIFICTATION
            If (CHECKISMOBILE(URL)) Then
                URLVISITEDMOBILEPOOL.ADDTOQUEUE(URL)
                MOBILEPAGESIZE.Add(HTML.Length)
                MOBILEPAGEENCODE.Add(doc.Encoding)
                LIST1.Items.Add(URL + " MARKED AS MOBILE")
            Else
                URLVISITEDNONMOBILEPOOL.ADDTOQUEUE(URL)
                NONMOBILEPAGESIZE.Add(HTML.Length)
                NONMOBILEPAGEENCODE.Add(doc.Encoding)
                LIST1.Items.Add(URL + " MARKED AS NON MOBILE")
            End If

            'SAVE TO LOCAL

            WRITEFILE(Application.StartupPath + "\" + (INDEX + 1).ToString() + ".DATA", HTML)

        Catch ex As Exception
            'FAIL TO DOWNLOAD
            LIST1.Items.Add("NETWORK ERROR, FAIL TO LOAD: " + URL + "  TRY NEXT")
        End Try
    End Sub

    Private Function CHECKISMOBILE(URL As String) As Boolean

        For Each o As String In MOBILEURL
            If URL.ToLower().Contains(o.ToLower()) Then
                Return True
            End If
        Next
        Return False
    End Function

    Private Sub WRITEFILE(FILENAME As String, FILECONTENT As String)
        Using sw As StreamWriter = New StreamWriter(FILENAME)
            ' Add some text to the file.
            sw.Write(FILECONTENT)
            sw.Close()
        End Using
    End Sub

    Private Function CHECKURLVALID(URL As String) As Boolean
        Dim GETRETURN As Boolean = False

        If Not URLPOOL.CHECKINSIDEQUEUE(URL) And URL.Contains(URLHOST) And Not URL.Contains("#") Then
            GETRETURN = True
        End If

        Return GETRETURN
    End Function

    Private Function GETURLHOST(URL As String) As String
        Try
            Dim _
                REGEX As _
                    New Regex(
                        "[a-z][a-z0-9+\-.]*://([a-z0-9\-._~%!$&'()*+,;=]+@)?(?<host>[a-z0-9\-._~%]+|\[[a-z0-9\-._~%!$&'()*+,;=:]+\])",
                        RegexOptions.IgnoreCase)
            Dim MATCHRESULT As Match = REGEX.Match(URL)
            Return MATCHRESULT.ToString()
        Catch ex As Exception
            Return ""
        End Try
    End Function

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

    Private Sub LIST1_MouseDown(sender As Object, e As MouseEventArgs) Handles LIST1.MouseDown
        Return
    End Sub

    Private Sub WebBrowser1_DocumentCompleted(sender As Object, e As WebBrowserDocumentCompletedEventArgs) _
        Handles WebBrowser1.DocumentCompleted
        FULLLOAD = True
        Timer1.Stop()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Button1.Enabled = False
        Button3.Enabled = False
        STARTJOB()
        Button1.Enabled = True
        Button3.Enabled = True
        Button2.Visible = True
        LIST1.Items.Add("JOB DONE!")
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Dim ARFF As String
        ARFF = "% 1. Title: ZHANGPENG ASSIGNMENT" + vbNewLine +
               "%" + vbNewLine +
               "% 2. Sources:" + vbNewLine +
               "%      (a) Creator: zhangpeng ID: s3280182 " + vbNewLine +
               "%      (b) ASSIGNMENT: RMIT ASSIGNMENT" + vbNewLine +
               "%      (c) Date: MAY, 2013" + vbNewLine +
               "% " + vbNewLine +
               "@RELATION CRAWL" + vbNewLine +
               "@ATTRIBUTE URL  STRING" + vbNewLine +
               "@ATTRIBUTE LENGTH  NUMERIC" + vbNewLine +
               "@ATTRIBUTE ENCODE  STRING" + vbNewLine +
               "@ATTRIBUTE class        {CRAWL-MOBILE,CRAWL-NOTMOBILE}" + vbNewLine +
               "@DATA" + vbNewLine
        'GET MOBILE INFORMATION
        For i As Integer = 0 To URLVISITEDMOBILEPOOL.GETFULLQUEUE().Count - 1
            ARFF = ARFF + """" + URLVISITEDMOBILEPOOL.GETFULLQUEUE(i) + """," + MOBILEPAGESIZE(i) + ",""" +
                   MOBILEPAGEENCODE(i) + """,CRAWL-MOBILE" + vbNewLine
        Next

        For i As Integer = 0 To URLVISITEDNONMOBILEPOOL.GETFULLQUEUE().Count - 1
            ARFF = ARFF + """" + URLVISITEDNONMOBILEPOOL.GETFULLQUEUE(i) + """," + NONMOBILEPAGESIZE(i) + ",""" +
                   NONMOBILEPAGEENCODE(i) + """,CRAWL-NOTMOBILE" + vbNewLine
        Next

        SaveFileDialog1.FileName = "CRAWL.arff"

        If SaveFileDialog1.ShowDialog() = DialogResult.OK Then
            WRITEFILE(SaveFileDialog1.FileName, ARFF)
        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        Form2.ShowDialog()
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        'TIME OUT
        TICKET = True
    End Sub
End Class

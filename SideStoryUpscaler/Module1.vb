Imports System.Drawing
Imports System.IO
Imports System.Threading


Module Module1
    Dim Logs As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(Environment.CurrentDirectory & "\SSU-log.txt", True, System.Text.Encoding.UTF8)

    Sub Main()
        Logs.AutoFlush = True
        Dim choice, ScaleFraction As String
        Dim ScaleFactor As Double
        Dim Failsafe As Boolean
        Dim cudnn As Boolean = False
        Dim mode As String
        Logs.WriteLine(vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & DateTime.Now.ToShortDateString & " - " & Now.TimeOfDay.ToString & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf & vbCrLf)
        Console.WriteLine("Waifu2x (which is used to upscale the images) requires the Microsoft Visual C++ 2015 Redistributable Update 3")
        Console.WriteLine("If you don't have it installed (or if you don't know) just run the vc.exe file that's in this same folder.")
        Console.WriteLine("Press Enter to continue.")
        Console.ReadLine()
        Do
            Console.WriteLine("Please enter the number corresponding to your resolution then press Enter to confirm.")
            Console.WriteLine("1) 1080p")
            Console.WriteLine("2) 720p")
            choice = Console.ReadLine()
            If choice = "1" Then
                ScaleFactor = 1.8
                ScaleFraction = "18/10"
                Console.Clear()
                Exit Do
            ElseIf choice = "2" Then
                ScaleFactor = 1.5
                ScaleFraction = "18/10"
                Console.Clear()
                Exit Do
            End If
            Console.Clear()
        Loop

        Do
            Console.WriteLine("Use GPU acceleration? (Note: Requires an Nvidia GPU and CUDA, run cuda.exe to install it)")
            Console.WriteLine("1) Yes")
            Console.WriteLine("2) No")
            choice = Console.ReadLine()
            If choice = "1" Then
                Failsafe = False
                Console.Clear()
                Exit Do
            ElseIf choice = "2" Then
                Failsafe = True
                Console.Clear()
                Exit Do
            End If
            Console.Clear()
        Loop

        If Command() <> "" Then
            If Command().Contains("/cudnn") Then
                cudnn = True
                Console.WriteLine("CUDNN acceleration enabled.")
            End If
        End If

        If cudnn Then
            mode = "cudnn"
        ElseIf Failsafe Then
            mode = "cpu"
        Else
            mode = "gpu"
        End If

        Dim di As New DirectoryInfo(Environment.CurrentDirectory)
        Dim count As Integer = 0
        For Each subfolder As DirectoryInfo In di.GetDirectories("*", SearchOption.AllDirectories)
            count = 0
            Dim fiArr As FileInfo() = subfolder.GetFiles()
            If Not (File.Exists(subfolder.FullName & "\upscaled")) And Not (subfolder.FullName.Contains("Magick")) Then
                Dim backgrounds As New List(Of String)

                If File.Exists(subfolder.FullName & "\story.ini") Then
                    Dim text() As String = File.ReadAllLines(subfolder.FullName & "\story.ini")
                    For i = 0 To text.Length - 1
                        text(i) = text(i).Replace(".bmp", ".png").Replace(".jpg", ".png").Replace(".jpeg", ".png").Replace("�@", " ")

                        If text(i).Contains("bg,") And text(i).Contains(".") Then
                            backgrounds.Add(text(i).Substring(text(i).IndexOf(",") + 1, text(i).IndexOf(".", text(i).IndexOf(",") + 1) - text(i).IndexOf(",") - 1))
                        End If

                        If text(i).Contains("haikei") And text(i).Contains(".") Then
                            backgrounds.Add(text(i).Substring(text(i).IndexOf("""") + 1, text(i).IndexOf(".") - text(i).IndexOf("""") - 1))
                        End If
                        Try
                            If text(i).Substring(0, 4) = "name" Then
                                If text(i).Chars(text(i).Length - 2) = " " Then
                                    text(i) = text(i).Substring(0, text(i).LastIndexOf(" ")) & """"
                                End If
                            End If
                        Catch ex As Exception

                        End Try

                    Next

                    backgrounds = backgrounds.Distinct.ToList()

                    File.Delete(subfolder.FullName & "\story.ini")
                    File.WriteAllLines(subfolder.FullName & "\story.ini", text)
                End If


                For Each fri In fiArr
                    If fri.IsReadOnly Then
                        fri.IsReadOnly = False
                    End If
                    count = count + 1
                    If backgrounds.Contains(fri.Name.Substring(0, fri.Name.Length - 4)) Then
                        RunCommandH("""" & Environment.CurrentDirectory & "\waifu2x\waifu2x-caffe-cui.exe""", "-p " & mode & " -s " & ScaleFactor & " --Model_dir """ & Environment.CurrentDirectory & "\waifu2x\models\anime_style_art_rgb"" -i """ & fri.FullName & """ -o """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""")
                        If fri.Extension <> ".png" Then
                            fri.Delete()
                        End If
                    Else
                        If MaskCheck(fri) Then
                            SplitAndMerge(fri)
                            RunCommandH("""" & Environment.CurrentDirectory & "\waifu2x\waifu2x-caffe-cui.exe""", "-p " & mode & " -s " & ScaleFactor & " --Model_dir """ & Environment.CurrentDirectory & "\waifu2x\models\anime_style_art_rgb"" -i """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""" & " -o """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""")
                            If fri.Extension <> ".png" Then
                                fri.Delete()
                            End If


                        ElseIf HasTransparency(fri) Then
                            RunCommandH("""" & Environment.CurrentDirectory & "\waifu2x\waifu2x-caffe-cui.exe""", "-p " & mode & " -s " & ScaleFactor & " --Model_dir """ & Environment.CurrentDirectory & "\waifu2x\models\anime_style_art_rgb"" -i """ & fri.FullName & """ -o """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""")
                            If fri.Extension <> ".png" Then
                                fri.Delete()
                            End If

                        ElseIf BGCheck(fri).Bool Then
                            Dim bmp As New Bitmap(fri.FullName)
                            Dim x, y As Double
                            y = 0
                            If BGCheck(fri).side = "right" Then
                                x = bmp.Width - 1
                            ElseIf BGCheck(fri).side = "left" Then
                                x = 0
                            End If
                            Dim color As String = GetRGB(fri.FullName, x, y)
                            bmp.Dispose()
                            RunCommandH("""" & Environment.CurrentDirectory & "\ImageMagick\magick.exe""", "convert """ & fri.FullName & """ -fuzz 0% -transparent """ & color & """ """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""")

                            fri = New FileInfo(fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png")
                            RunCommandH("""" & Environment.CurrentDirectory & "\waifu2x\waifu2x-caffe-cui.exe""", "-p " & mode & " -s " & ScaleFactor & " --Model_dir """ & Environment.CurrentDirectory & "\waifu2x\models\anime_style_art_rgb"" -i """ & fri.FullName & """ -o """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""")
                            If fri.Extension <> ".png" Then
                                fri.Delete()
                            End If
                        ElseIf fri.Extension = ".png" Or fri.Extension = ".jpg" Or fri.Extension = ".jpeg" Or fri.Extension = ".bmp" Then
                            Dim answer As Integer = 0
                            Do Until answer = 1 Or answer = 2 Or answer = 3
                                Console.Clear()
                                Console.WriteLine("Does the image have (write the number corresponding to your answer):")
                                Console.WriteLine("1) A right side that functions as a mask (green except for where the character/CG is)")
                                Console.WriteLine("2) Some color it uses to distinguish the background from the character (if it seemingly has no background, it's probably using white as the color)")
                                Console.WriteLine("3) Neither, it's a background or a full screen CG/image (it should have the background included in the image)")
                                Console.WriteLine("You can find examples of all of these under the ""Sample"" folder.")
                                Console.WriteLine("Note: The whitespace surrounding the image is not a mask or anything of the sort, it's simply empty space. You can find an example of that in the folder as well.")
                                If Process.GetProcessesByName("IMDisplay").Length < 1 Then
                                    RunCommandNW("""" & Environment.CurrentDirectory & "\ImageMagick\IMDisplay.exe""", """" & fri.FullName & """")
                                End If


                                Try
                                    answer = Console.ReadLine()
                                Catch ex As Exception

                                End Try
                            Loop
                            Console.Clear()

                            Try
                                For Each p As Process In Process.GetProcessesByName("IMDisplay")
                                    p.Kill()
                                Next
                            Catch ex As Exception

                            End Try


                            If answer = 1 Then
                                SplitAndMerge(fri)
                                If fri.Extension <> ".png" Then
                                    fri.Delete()
                                End If
                                fri = New FileInfo(fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png")
                            ElseIf answer = 2 Then
                                answer = 0
                                Do
                                    Console.Clear()
                                    Console.WriteLine("Is the background color present in:")
                                    Console.WriteLine("1) The upper left corner")
                                    Console.WriteLine("2) The upper right corner")
                                    Console.WriteLine("If it's in both just choose one, it doesn't matter.")
                                    Console.WriteLine("If it's in neither, come tell me in the NG+ discord because it means I don't actually get how it works.")
                                    If Process.GetProcessesByName("IMDisplay").Length < 1 Then
                                        RunCommandNW("""" & Environment.CurrentDirectory & "\ImageMagick\IMDisplay.exe""", """" & fri.FullName & """")
                                    End If

                                    Try
                                        answer = Console.ReadLine

                                    Catch ex As Exception

                                    End Try
                                Loop Until answer = 1 Or answer = 2
                                Console.Clear()

                                Try
                                    For Each p As Process In Process.GetProcessesByName("IMDisplay")
                                        p.Kill()
                                    Next
                                Catch ex As Exception

                                End Try


                                Dim bmp As New Bitmap(fri.FullName)
                                Dim x, y As Double
                                y = 0
                                If answer = 1 Then
                                    x = 0
                                ElseIf answer = 2 Then
                                    x = bmp.Width - 1
                                End If
                                Dim color As String = GetRGB(fri.FullName, x, y)
                                bmp.Dispose()
                                RunCommandH("""" & Environment.CurrentDirectory & "\ImageMagick\magick.exe""", "convert """ & fri.FullName & """ -fuzz 0% -transparent """ & color & """ """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""")
                                fri.Delete()
                                fri = New FileInfo(fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png")
                            End If
                            RunCommandH("""" & Environment.CurrentDirectory & "\waifu2x\waifu2x-caffe-cui.exe""", "-p " & mode & " -s " & ScaleFactor & " --Model_dir """ & Environment.CurrentDirectory & "\waifu2x\models\anime_style_art_rgb"" -i """ & fri.FullName & """ -o """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""")

                            If fri.Extension <> ".png" Then
                                fri.Delete()
                            End If
                        End If
                    End If
                    Console.WriteLine("File " & count + 1 & " out of " & fiArr.GetLength(0) + 1 & " completed")
                Next fri
                File.WriteAllText(subfolder.FullName & "\upscaled", "Already upscaled. Ignore this folder.")
                Console.WriteLine("Folder " & subfolder.Name & " completed")
            End If
        Next
        If File.Exists(Environment.CurrentDirectory & "\disclaimershown.ini") Then
            File.Delete(Environment.CurrentDirectory & "\disclaimershown.ini")
        End If

        File.WriteAllText(Environment.CurrentDirectory & "\disclaimershown.ini", "[disclaimer]" & vbCrLf & "shown=""true""")
        Logs.Close()
    End Sub


    Function BGCheck(fri As FileInfo) As CustomType
        Dim result As New CustomType
        If fri.Extension <> ".png" And fri.Extension <> ".jpg" And fri.Extension <> ".jpeg" And fri.Extension <> ".bmp" Then
            result.Bool = False
            result.side = ""
            Return result
        End If
        Dim bmp As New Bitmap(fri.FullName)

        Dim c(4) As String
        c(0) = GetRGB(fri.FullName, 0, 0)
        c(1) = GetRGB(fri.FullName, 0, bmp.Height - 1)
        c(2) = GetRGB(fri.FullName, bmp.Width - 1, bmp.Height - 1)
        c(3) = GetRGB(fri.FullName, bmp.Width - 1, 0)
        Dim counter As Integer = 0

        For i = 0 To 4
            counter = 0
            For j = 0 To 4
                If i = j Then
                    j = j + 1
                Else
                    If c(i) = c(j) Then
                        counter = counter + 1
                        If counter = 2 Then

                            result.Bool = True
                            If c(i) = c(0) Then
                                result.side = "left"
                            ElseIf c(i) = c(3) Then
                                result.side = "right"
                            End If
                            bmp.Dispose()
                            Return result
                        End If
                    End If
                End If
            Next
        Next
        bmp.Dispose()
        result.Bool = False
        result.side = ""
        Return result

    End Function

    Class CustomType
        Public Bool As Boolean
        Public side As String
    End Class

    Function MaskCheck(fri As FileInfo) As Boolean

        If fri.Extension <> ".bmp" Then
            Return False
        End If

        If fri.FullName.Contains("black.bmp") Or fri.FullName.Contains("Marco1") Then
            Return True
        End If
        Dim bmp As New Bitmap(fri.FullName)
        Dim FileNameOnly As String = fri.FullName.Substring(0, fri.FullName.Length - 4)
        Dim Corner1, Corner2, Corner3, Corner4 As Boolean

        If Getcolor(fri.FullName, "Red", 0, 0) = 0 And Getcolor(fri.FullName, "Green", 0, 0) = 255 And Getcolor(fri.FullName, "Blue", 0, 0) = 0 Then
            Corner1 = True
        Else
            Corner1 = False
        End If

        If Getcolor(fri.FullName, "Red", 0, bmp.Height - 1) = 0 And Getcolor(fri.FullName, "Green", 0, bmp.Height - 1) = 255 And Getcolor(fri.FullName, "Blue", 0, bmp.Height - 1) = 0 Then
            Corner2 = True
        Else
            Corner2 = False
        End If

        If Getcolor(fri.FullName, "Red", bmp.Width / 2 - 1, 0) = 0 And Getcolor(fri.FullName, "Green", bmp.Width / 2 - 1, 0) = 255 And Getcolor(fri.FullName, "Blue", bmp.Width / 2 - 1, 0) = 0 Then
            Corner3 = True
        Else
            Corner3 = False
        End If

        If Getcolor(fri.FullName, "Red", bmp.Width / 2 - 1, bmp.Height - 1) = 0 And Getcolor(fri.FullName, "Green", bmp.Width / 2 - 1, bmp.Height - 1) = 255 And Getcolor(fri.FullName, "Blue", bmp.Width / 2 - 1, bmp.Height - 1) = 0 Then
            Corner4 = True
        Else
            Corner4 = False
        End If

        bmp.Dispose()
        If Corner1 Or Corner2 Or Corner3 Or Corner4 Then
            Return True
        Else
            Return False
        End If

    End Function

    Function HasTransparency(fri As FileInfo) As Boolean
        If fri.Extension <> ".png" And fri.Extension <> ".bmp" Then
            Return False
        End If
        Dim output As String = RunCommandH("""" & Environment.CurrentDirectory & "\ImageMagick\magick.exe""", "convert """ & fri.FullName & """ -verbose info:")
        Try
            output = output.Substring(output.IndexOf("Channel statistics:"), output.IndexOf("Image statistics:") - output.IndexOf("Channel statistics:"))
        Catch ex As Exception
            Return False
        End Try


        If output.Contains("Alpha") Then
            output = output.Substring(output.IndexOf("Alpha"), output.LastIndexOf("kurtosis") - output.IndexOf("Alpha"))

            Dim sd As Double
            Try
                sd = Convert.ToDouble(output.Substring(output.IndexOf("standard deviation:") + 19, output.LastIndexOf("(") - output.IndexOf("standard deviation:") - 19))
            Catch ex As Exception
                Try
                    Dim min, max, mean As Double
                    min = Convert.ToDouble(output.Substring(output.IndexOf("min:") + 4, output.IndexOf("(", output.IndexOf("min:")) - output.IndexOf("min:") - 4))
                    max = Convert.ToDouble(output.Substring(output.IndexOf("max:") + 4, output.IndexOf("(", output.IndexOf("min:")) - output.IndexOf("max:") - 4))
                    mean = Convert.ToDouble(output.Substring(output.IndexOf("mean:") + 5, output.IndexOf("(", output.IndexOf("mean:")) - output.IndexOf("mean:") - 5))
                    If min = max And max = mean Then
                        Return False
                    Else
                        Return True
                    End If
                Catch ex2 As Exception
                    Return False
                End Try
            End Try

            If sd <> 0 Then
                Return True
            Else
                Return False
            End If
        Else
            Return False
        End If

    End Function



    Sub SplitAndMerge(fri As FileInfo)
        Dim filenameonly As String = fri.FullName.Substring(0, fri.FullName.Length - 4)
        RunCommandH("""" & Environment.CurrentDirectory & "\ImageMagick\magick.exe""", "convert -crop 50%x100% """ & fri.FullName & """ """ & filenameonly & "_%d.png""")
        RunCommandH("""" & Environment.CurrentDirectory & "\ImageMagick\magick.exe""", "convert """ & filenameonly & "_0.png"" ( """ & filenameonly & "_1.png"" -colorspace gray -threshold 0 -alpha off -negate ) -compose copy-opacity -composite """ & fri.FullName.Substring(0, fri.FullName.Length - 4) & ".png""")

        File.Delete(filenameonly & "_1.png")
        File.Delete(filenameonly & "_0.png")
        If fri.Extension <> ".png" Then
            File.Delete(fri.FullName)
        End If
    End Sub

    Function Getcolor(File As String, Color As String, X As Double, Y As Double) As Int32
        Select Case Color
            Case "Red"
                Color = "r"
            Case "Green"
                Color = "g"
            Case "Blue"
                Color = "b"
        End Select
        Return Convert.ToInt32(RunCommandH("""" & Environment.CurrentDirectory & "\ImageMagick\magick.exe""", "convert """ & File & """[1x1+" & X & "+" & Y & "] -format ""%[fx: floor(255 * u." & Color & ")]"" info:"))
    End Function

    Function GetRGB(File As String, X As Double, Y As Double) As String
        Dim r, g, b As Integer
        r = Getcolor(File, "Red", X, Y)
        g = Getcolor(File, "Green", X, Y)
        b = Getcolor(File, "Blue", X, Y)
        Return "rgb(" & r & "," & g & "," & b & ")"
    End Function

    Function RunCommandH(Command As String, Arguments As String) As String
        'Console.WriteLine(Command)
        Logs.WriteLine(Command & Arguments)
        'Console.ReadLine()
        Dim oProcess As New Process()
        Dim oStartInfo As New ProcessStartInfo(Command, Arguments)
        oStartInfo.UseShellExecute = False
        oStartInfo.RedirectStandardOutput = True
        oProcess.StartInfo = oStartInfo
        Try
            oProcess.Start()

        Catch ex As Exception
            File.WriteAllText(Environment.CurrentDirectory & "crashreport.txt", ex.ToString)
            Console.WriteLine("Something went wrong.")
            Console.WriteLine("Please message me on discord (you can find me on the NG+ server, check ecstasywastaken.blogspot.com for the link) and send me the crashreport.txt file.")
            Console.ReadLine()
        End Try



        Dim sOutput As String
        Using oStreamReader As System.IO.StreamReader = oProcess.StandardOutput
            sOutput = oStreamReader.ReadToEnd()
        End Using
        If Not Arguments.Contains("verbose") Or Arguments.Contains("[1x1") Then
            Logs.Writeline(sOutput)
            'Console.WriteLine(sOutput)
        End If


        oProcess.WaitForExit()
        Return sOutput
        oProcess.WaitForExit()

        oProcess.Dispose()



    End Function

    Sub RunCommandNW(Command As String, Arguments As String)
        'Console.WriteLine(Command)
        'Console.ReadLine()
        Dim oProcess As New Process()
        Dim oStartInfo As New ProcessStartInfo(Command, Arguments)
        oStartInfo.UseShellExecute = False
        oStartInfo.RedirectStandardOutput = True
        oProcess.StartInfo = oStartInfo
        oProcess.Start()

        oProcess.Dispose()
    End Sub

End Module

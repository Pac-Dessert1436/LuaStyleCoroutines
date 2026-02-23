Imports System.Runtime.CompilerServices

Public Module CoroutineExtensions
    ''' <summary>
    ''' Projects each element of a coroutine into a new form.
    ''' </summary>
    <Extension>
    Public Function [Select](Of TSource, TResult)(
            coroutine As Coroutine(Of TSource),
            selector As Func(Of TSource, TResult)) As Coroutine(Of TResult)
        ArgumentNullException.ThrowIfNull(coroutine)
        ArgumentNullException.ThrowIfNull(selector)

        Dim coroFC = coroutine.FreshCopy()
        Return New Coroutine(Of TResult)(Function()
                                             Return Iterator Function()
                                                        coroFC.Start()
                                                        ' Start the iteration
                                                        While coroFC.Continue()
                                                            Yield selector(coroFC.Current)
                                                        End While
                                                    End Function()
                                         End Function)
    End Function


    ''' <summary>
    ''' Creates a coroutine that yields elements with their indices.
    ''' </summary>
    <Extension>
    Public Function [Select](Of T)(coroutine As Coroutine(Of T)) As Coroutine(Of (Index As Integer, Value As T))
        ArgumentNullException.ThrowIfNull(coroutine)
        Dim coroFC = coroutine.FreshCopy()
        Return New Coroutine(Of (Integer, T))(Function()
                                                  Return Iterator Function()
                                                             coroFC.Start()
                                                             Dim index = 0
                                                             While coroFC.Continue()
                                                                 Yield (index, coroFC.Current)
                                                                 index += 1
                                                             End While
                                                         End Function()
                                              End Function)
    End Function

    ''' <summary>
    ''' Filters a coroutine based on a predicate.
    ''' </summary>
    <Extension>
    Public Function Where(Of T)(coroutine As Coroutine(Of T), predicate As Func(Of T, Boolean)) As Coroutine(Of T)
        ArgumentNullException.ThrowIfNull(coroutine)
        ArgumentNullException.ThrowIfNull(predicate)
        Dim coroFC = coroutine.FreshCopy()
        Return New Coroutine(Of T)(Function()
                                       Return Iterator Function()
                                                  coroFC.Start()
                                                  While coroFC.Continue()
                                                      If predicate(coroFC.Current) Then
                                                          Yield coroFC.Current
                                                      End If
                                                  End While
                                              End Function()
                                   End Function)
    End Function

    ''' <summary>
    ''' Takes the first N elements from a coroutine.
    ''' </summary>
    <Extension>
    Public Function Take(Of T)(coroutine As Coroutine(Of T), count As Integer) As Coroutine(Of T)
        ArgumentNullException.ThrowIfNull(coroutine)
        ArgumentOutOfRangeException.ThrowIfNegative(count)
        Dim coroFC = coroutine.FreshCopy()

        Return New Coroutine(Of T)(Function()
                                       Return Iterator Function()
                                                  coroFC.Start()
                                                  Dim taken = 0
                                                  While taken < count AndAlso coroFC.Continue()
                                                      Yield coroFC.Current
                                                      taken += 1
                                                  End While
                                              End Function()
                                   End Function)
    End Function

    ''' <summary>
    ''' Skips the first N elements from a coroutine.
    ''' </summary>
    <Extension>
    Public Function Skip(Of T)(
            coroutine As Coroutine(Of T),
            count As Integer) As Coroutine(Of T)
        ArgumentNullException.ThrowIfNull(coroutine)
        ArgumentOutOfRangeException.ThrowIfNegative(count)
        Dim coroFC = coroutine.FreshCopy()
        
        Return New Coroutine(Of T)(Function()
                                       Return Iterator Function()
                                                  coroFC.Start()
                                                  Dim skipped = 0
                                                  While coroFC.Continue()
                                                      If skipped >= count Then
                                                          Yield coroFC.Current
                                                      End If
                                                      skipped += 1
                                                  End While
                                              End Function()
                                   End Function)
    End Function

    ''' <summary>
    ''' Creates a coroutine that yields elements as long as a condition is true.
    ''' </summary>
    <Extension>
    Public Function TakeWhile(Of T)(
            coroutine As Coroutine(Of T),
            predicate As Func(Of T, Boolean)) As Coroutine(Of T)
        ArgumentNullException.ThrowIfNull(coroutine)
        ArgumentNullException.ThrowIfNull(predicate)
        Dim coroFC = coroutine.FreshCopy()
        Return New Coroutine(Of T)(Function()
                                       Return Iterator Function()
                                                  coroFC.Start()
                                                  While coroFC.Continue() AndAlso predicate(coroFC.Current)
                                                      Yield coroFC.Current
                                                  End While
                                              End Function()
                                   End Function)
    End Function

    ''' <summary>
    ''' Bypasses elements in a coroutine as long as a condition is true.
    ''' </summary>
    <Extension>
    Public Function SkipWhile(Of T)(
            coroutine As Coroutine(Of T),
            predicate As Func(Of T, Boolean)) As Coroutine(Of T)
        ArgumentNullException.ThrowIfNull(coroutine)
        ArgumentNullException.ThrowIfNull(predicate)
        Dim coroFC = coroutine.FreshCopy()
        Return New Coroutine(Of T)(Function()
                                       Return Iterator Function()
                                                  coroFC.Start()
                                                  Dim skipping = True
                                                  While coroFC.Continue()
                                                      If skipping AndAlso Not predicate(coroFC.Current) Then
                                                          skipping = False
                                                      End If
                                                      If Not skipping Then
                                                          Yield coroFC.Current
                                                      End If
                                                  End While
                                              End Function()
                                   End Function)
    End Function

    ''' <summary>
    ''' Concatenates two coroutines.
    ''' </summary>
    <Extension>
    Public Function Concat(Of T)(first As Coroutine(Of T), second As Coroutine(Of T)) As Coroutine(Of T)
        ArgumentNullException.ThrowIfNull(first)
        ArgumentNullException.ThrowIfNull(second)
        Dim firstFC = first.FreshCopy()
        Dim secondFC = second.FreshCopy()
        Return New Coroutine(Of T)(Function()
                                       Return Iterator Function()
                                                  firstFC.Start()
                                                  While firstFC.Continue()
                                                      Yield firstFC.Current
                                                  End While

                                                  secondFC.Start()
                                                  While secondFC.Continue()
                                                      Yield secondFC.Current
                                                  End While
                                              End Function()
                                   End Function)
    End Function

    ''' <summary>
    ''' Transforms each element of a coroutine into a new coroutine and flattens the results.
    ''' </summary>
    <Extension>
    Public Function SelectMany(Of TSource, TResult)(
            source As Coroutine(Of TSource),
            selector As Func(Of TSource, Coroutine(Of TResult))) As Coroutine(Of TResult)
        ArgumentNullException.ThrowIfNull(source)
        ArgumentNullException.ThrowIfNull(selector)
        Dim sourceFC = source.FreshCopy()
        Return New Coroutine(Of TResult)(Function()
                                             Return Iterator Function()
                                                        sourceFC.Start()
                                                        While sourceFC.Continue()
                                                            Dim innerFC = selector(sourceFC.Current).FreshCopy()
                                                            innerFC.Start()
                                                            While innerFC.Continue()
                                                                Yield innerFC.Current
                                                            End While
                                                        End While
                                                    End Function()
                                         End Function)
    End Function

    ''' <summary>
    ''' Returns distinct elements from a coroutine.
    ''' </summary>
    <Extension>
    Public Function Distinct(Of T)(coroutine As Coroutine(Of T)) As Coroutine(Of T)
        ArgumentNullException.ThrowIfNull(coroutine)
        Dim coroFC = coroutine.FreshCopy()
        Return New Coroutine(Of T)(Function()
                                       Return Iterator Function()
                                                  Dim seen As New HashSet(Of T)
                                                  coroFC.Start()
                                                  While coroFC.Continue()
                                                      If seen.Add(coroFC.Current) Then Yield coroFC.Current
                                                  End While
                                              End Function()
                                   End Function)
    End Function

    ''' <summary>
    ''' Converts a coroutine to a List.
    ''' </summary>
    <Extension>
    Public Function ToList(Of T)(coroutine As Coroutine(Of T)) As List(Of T)
        ArgumentNullException.ThrowIfNull(coroutine)
        Dim result As New List(Of T)
        Dim coroFC = coroutine.FreshCopy()
        coroFC.Start()
        While coroFC.Continue()
            result.Add(coroFC.Current)
        End While
        Return result
    End Function

    ''' <summary>
    ''' Converts a coroutine to an Array.
    ''' </summary>
    <Extension>
    Public Function ToArray(Of T)(coroutine As Coroutine(Of T)) As T()
        Return coroutine.ToList().ToArray()
    End Function

    ''' <summary>
    ''' Returns the first element of a coroutine.
    ''' </summary>
    <Extension>
    Public Function First(Of T)(coroutine As Coroutine(Of T)) As T
        ArgumentNullException.ThrowIfNull(coroutine)
        Dim coroFC = coroutine.FreshCopy()
        coroFC.Start()
        If coroFC.Continue() Then Return coroFC.Current
        Throw New InvalidOperationException("Coroutine contains no elements")
    End Function

    ''' <summary>
    ''' Returns the first element of a coroutine, or a default value if empty.
    ''' </summary>
    <Extension>
    Public Function FirstOrDefault(Of T)(coroutine As Coroutine(Of T)) As T
        ArgumentNullException.ThrowIfNull(coroutine)
        Dim coroFC = coroutine.FreshCopy()
        coroFC.Start()
        If coroFC.Continue() Then Return coroFC.Current
        Return Nothing
    End Function

    ''' <summary>
    ''' Aggregates the elements of a coroutine.
    ''' </summary>
    <Extension>
    Public Function Aggregate(Of T, TAccumulate)(
            coroutine As Coroutine(Of T),
            seed As TAccumulate,
            func As Func(Of TAccumulate, T, TAccumulate)) As TAccumulate
        ArgumentNullException.ThrowIfNull(coroutine)
        ArgumentNullException.ThrowIfNull(func)
        Dim result = seed
        Dim coroFC = coroutine.FreshCopy()
        coroFC.Start()
        While coroFC.Continue()
            result = func(result, coroFC.Current)
        End While
        Return result
    End Function

    ''' <summary>
    ''' Zips two coroutines together.
    ''' </summary>
    <Extension>
    Public Function Zip(Of TFirst, TSecond, TResult)(
            first As Coroutine(Of TFirst),
            second As Coroutine(Of TSecond),
            resultSelector As Func(Of TFirst, TSecond, TResult)) As Coroutine(Of TResult)
        ArgumentNullException.ThrowIfNull(first)
        ArgumentNullException.ThrowIfNull(second)
        ArgumentNullException.ThrowIfNull(resultSelector)
        Dim firstFC = first.FreshCopy()
        Dim secondFC = second.FreshCopy()
        Return New Coroutine(Of TResult)(Function()
                                             Return Iterator Function()
                                                        firstFC.Start()
                                                        secondFC.Start()

                                                        Dim firstHasValue As Boolean
                                                        Dim secondHasValue As Boolean

                                                        Do
                                                            firstHasValue = firstFC.Continue()
                                                            secondHasValue = secondFC.Continue()

                                                            If firstHasValue AndAlso secondHasValue Then
                                                                Yield resultSelector(firstFC.Current, secondFC.Current)
                                                            End If
                                                        Loop While firstHasValue AndAlso secondHasValue
                                                    End Function()
                                         End Function)
    End Function
End Module
''' <summary>
''' Provides static methods for coroutine management, similar to Lua's coroutine module.
''' </summary>
Public Module CoroutineManager
    ' Thread-local storage for tracking the currently running coroutine
    Private _currentCoroutine As Object = Nothing
    Private _isMainCoroutine As Boolean = True

    ''' <summary>
    ''' Gets the currently running coroutine and whether it's the main coroutine.
    ''' Similar to Lua's coroutine.running().
    ''' </summary>
    ''' <returns>A tuple containing the current coroutine (or Nothing if main), 
    ''' and a boolean indicating if it's the main coroutine.</returns>
    Public Function Running() As (coroutine As Object, isMain As Boolean)
        Return (_currentCoroutine, _isMainCoroutine)
    End Function

    ''' <summary>
    ''' Checks if the specified coroutine can yield. If no coroutine is specified, 
    ''' checks the current one. Similar to Lua's coroutine.isyieldable().
    ''' </summary>
    ''' <param name="coroutine">The coroutine to check (optional, defaults to current).</param>
    ''' <returns>True if the coroutine can yield, False otherwise.</returns>
    Public Function IsYieldable(Optional coroutine As Object = Nothing) As Boolean
        ' If no coroutine specified, check the current one
        If coroutine Is Nothing Then coroutine = _currentCoroutine

        ' Main coroutine cannot yield
        If coroutine Is Nothing OrElse _isMainCoroutine Then Return False

        ' Check if the coroutine is running
        If TypeOf coroutine Is Coroutine Then Return DirectCast(coroutine, Coroutine).IsRunning

        ' For generic coroutines, we need to use reflection
        With coroutine.GetType()
            If .IsGenericType AndAlso .GetGenericTypeDefinition() = GetType(Coroutine(Of )) Then
                Dim isRunningProp = .GetProperty("IsRunning")
                If isRunningProp IsNot Nothing Then isRunningProp.GetValue(coroutine)
            End If
        End With

        Return False
    End Function

    ''' <summary>
    ''' Sets the current running coroutine. This is used internally by the Coroutine class.
    ''' </summary>
    ''' <param name="coroutine">The coroutine to set as current.</param>
    Friend Sub SetCurrentCoroutine(coroutine As Object)
        _currentCoroutine = coroutine
        _isMainCoroutine = coroutine Is Nothing
    End Sub
End Module
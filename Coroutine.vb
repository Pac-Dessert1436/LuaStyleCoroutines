''' <summary>
''' Delegate for a coroutine function that returns an IEnumerable(Of T).
''' </summary>
''' <typeparam name="T">Data type of the coroutine.</typeparam>
Public Delegate Function CoroutineFunc(Of T)() As IEnumerable(Of T)

''' <summary>
''' Delegate for a coroutine function that returns an IEnumerable(Of Object).
''' </summary>
Public Delegate Function CoroutineFunc() As IEnumerable(Of Object)

''' <summary>
''' Status of a coroutine.
''' </summary>
Public Enum CoroutineStatus As Byte
    ''' <summary>
    ''' The coroutine is not running.
    ''' </summary>
    Idle = 0
    ''' <summary>
    ''' The coroutine is running.
    ''' </summary>
    Running = 1
    ''' <summary>
    ''' The coroutine is force stopped.
    ''' </summary>
    ForceStopped = 2
    ''' <summary>
    ''' The coroutine is completed.
    ''' </summary>
    Completed = 3
End Enum

''' <summary>
''' A generic coroutine that manages the execution of a coroutine.
''' </summary>
''' <typeparam name="T">Data type of the coroutine.</typeparam>
Public Class Coroutine(Of T)
    Implements IDisposable
    Private _isDisposed As Boolean = False

    ' Store both the enumerator AND the factory
    Private _enumerator As IEnumerator(Of T)
    Private ReadOnly _enumeratorFactory As Func(Of IEnumerator(Of T))
    Private ReadOnly _sourceEnumerable As IEnumerable(Of T)

    ' Marks the status of the coroutine.
    Private _status As CoroutineStatus = CoroutineStatus.Idle
    Public Property Status As CoroutineStatus
        Get
            Return _status
        End Get
        Private Set(value As CoroutineStatus)
            _status = value
        End Set
    End Property

    ' Action to be performed when the coroutine is cleaned up.
    Private _cleanup As Action = Nothing
    Private _incomingData As T

    ''' <summary>
    ''' Sets the cleanup action to be performed when the coroutine is cleaned up.
    ''' </summary>
    Public WriteOnly Property Cleanup As Action
        Set(value As Action)
            _cleanup = value
        End Set
    End Property

    ''' <summary>
    ''' Whether the coroutine is idle.
    ''' </summary>
    Public ReadOnly Property IsIdle As Boolean
        Get
            Return Status = CoroutineStatus.Idle
        End Get
    End Property

    ''' <summary>
    ''' Whether the coroutine is running.
    ''' </summary>
    Public ReadOnly Property IsRunning As Boolean
        Get
            Return Status = CoroutineStatus.Running
        End Get
    End Property

    ''' <summary>
    ''' Whether the coroutine is stopped.
    ''' </summary>
    Public ReadOnly Property IsForceStopped As Boolean
        Get
            Return Status = CoroutineStatus.ForceStopped
        End Get
    End Property

    ''' <summary>
    ''' Whether the coroutine is completed.
    ''' </summary>
    Public ReadOnly Property IsCompleted As Boolean
        Get
            Return Status = CoroutineStatus.Completed
        End Get
    End Property

    ''' <summary>
    ''' Current value of the coroutine (returns default value when no data).
    ''' </summary>
    Public ReadOnly Property Current As T
        Get
            If _enumerator Is Nothing Then Return Nothing
            Return _enumerator.Current
        End Get
    End Property

    ''' <summary>
    ''' Creates a new coroutine from an iterable source.
    ''' </summary>
    ''' <param name="iterable">The iterable source to create the coroutine from.</param>
    Public Sub New(iterable As IEnumerable(Of T))
        ArgumentNullException.ThrowIfNull(iterable)
        _sourceEnumerable = iterable
        _enumeratorFactory = Function() iterable.GetEnumerator()
        _enumerator = _enumeratorFactory()
    End Sub

    ''' <summary>
    ''' Creates a new coroutine from a parameter array of values.
    ''' </summary>
    ''' <param name="iterable">The parameter array of values to create the coroutine from.</param>
    Public Sub New(ParamArray iterable As T())
        ArgumentNullException.ThrowIfNull(iterable)
        _sourceEnumerable = iterable.ToList()
        _enumeratorFactory = Function() _sourceEnumerable.GetEnumerator()
        _enumerator = _enumeratorFactory()
    End Sub

    ''' <summary>
    ''' Creates a new coroutine from a coroutine function.
    ''' </summary>
    ''' <param name="coroutineFunc">The coroutine function to create the coroutine from.</param>
    Public Sub New(coroutineFunc As CoroutineFunc(Of T))
        ArgumentNullException.ThrowIfNull(coroutineFunc)
        _sourceEnumerable = coroutineFunc()
        _enumeratorFactory = Function() _sourceEnumerable.GetEnumerator()
        _enumerator = _enumeratorFactory()
    End Sub

    ''' <summary>
    ''' Creates a new coroutine from a fresh copy of the source enumerable.
    ''' </summary>
    ''' <returns>A new coroutine instance.</returns>
    Public Function FreshCopy() As Coroutine(Of T)
        If _sourceEnumerable IsNot Nothing Then Return New Coroutine(Of T)(_sourceEnumerable)
        Throw New InvalidOperationException("Cannot create fresh copy from enumerator-only source")
    End Function

#Region "Coroutine Core Methods"
    ''' <summary>
    ''' Starts the coroutine by resetting its state.
    ''' </summary>
    Public Sub Start(Optional reset As Boolean = True)
        ' Resets the state of the coroutine
        If IsRunning Then Throw New InvalidOperationException("Coroutine is already running, cannot be started again")
       
        ' Create a fresh enumerator if possible
        If reset AndAlso _enumeratorFactory IsNot Nothing Then _enumerator = _enumeratorFactory()
        Status = CoroutineStatus.Running
        
        ' Set as current running coroutine
        CoroutineManager.SetCurrentCoroutine(Me)

        ' Do NOT execute Continue() here - let the caller decide when to start iteration
    End Sub

    ''' <summary>
    ''' Tries to reset the coroutine so it can be started again.
    ''' </summary>
    ''' <returns>True if reset was successful, False otherwise.</returns>
    Public Function TryReset() As Boolean
        If IsRunning Then Return False

        If _enumeratorFactory IsNot Nothing Then
            _enumerator = _enumeratorFactory()
            Status = CoroutineStatus.Idle
            Return True
        End If

        ' Try the old Reset method as fallback
        Try
            _enumerator.Reset()
            Return True
        Catch ex As NotSupportedException
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Executes the next step of the coroutine. DO NOT CALL THIS BEFORE STARTING THE COROUTINE.
    ''' </summary>
    ''' <returns>Whether there are more steps to execute.</returns>
    Public Function [Continue]() As Boolean
        If Not IsRunning Then Throw New InvalidOperationException("Coroutine is not running, cannot continue")
        ' If the coroutine is force-stopped or completed, return false.
        If IsForceStopped OrElse IsCompleted Then Return False

        Try
            ' Set as current running coroutine
            CoroutineManager.SetCurrentCoroutine(Me)
            
            ' Moves the enumerator to the next step.
            Dim hasNext = _enumerator.MoveNext()
            If Not hasNext Then
                Status = CoroutineStatus.Completed
                _cleanup?.Invoke()
                ' Clear current coroutine when completed
                CoroutineManager.SetCurrentCoroutine(Nothing)
            End If
            Return hasNext
        Catch
            ForceStop()
            Throw
        End Try
    End Function

    ''' <summary>
    ''' Forces the coroutine to stop, interrupting the execution.
    ''' </summary>
    Public Sub ForceStop()
        If IsForceStopped Then Exit Sub

        Status = CoroutineStatus.ForceStopped
        ' Executes the cleanup action.
        _cleanup?.Invoke()
        
        ' Clear current coroutine if this was the running one
        Dim current = CoroutineManager.Running().coroutine
        If ReferenceEquals(current, Me) Then
            CoroutineManager.SetCurrentCoroutine(Nothing)
        End If
    End Sub

    ''' <summary>
    ''' Terminates the coroutine, marking it as completed normally (no exception thrown).
    ''' </summary>
    Public Sub Terminate()
        If IsCompleted OrElse IsForceStopped Then Exit Sub

        Status = CoroutineStatus.Completed
        ' Executes the cleanup action.
        _cleanup?.Invoke()
        
        ' Clear current coroutine if this was the running one
        Dim current = CoroutineManager.Running().coroutine
        If ReferenceEquals(current, Me) Then
            CoroutineManager.SetCurrentCoroutine(Nothing)
        End If
    End Sub

    ''' <summary>
    ''' Resumes the coroutine with the given data.
    ''' </summary>
    ''' <param name="data">Data to be passed to the coroutine.</param>
    ''' <returns>Whether there are more steps to execute.</returns>
    Public Function ResumeWith(data As T) As Boolean
        _incomingData = data
        Return [Continue]()
    End Function

    ''' <summary>
    ''' Received data from the last step of the coroutine.
    ''' </summary>
    Public ReadOnly Property ReceivedData As T
        Get
            Return _incomingData
        End Get
    End Property
#End Region

    ''' <summary>
    ''' Stops the coroutine and disposes of the enumerator if it implements IDisposable.
    ''' </summary>
    Protected Overridable Sub Dispose(disposing As Boolean)
        If IsRunning Then Throw New InvalidOperationException("Coroutine is running, cannot dispose")

        If Not _isDisposed Then
            If disposing Then
                ' TODO: dispose managed state (managed objects)
                If _enumerator IsNot Nothing Then
                    Try
                        DirectCast(_enumerator, IDisposable)?.Dispose()
                    Catch
                        ' Ignore any exceptions
                    End Try
                End If
            End If

            ' TODO: free unmanaged resources (unmanaged objects) and override finalizer
            ' TODO: set large fields to null
            _isDisposed = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub

    ''' <summary>
    ''' Creates a coroutine that yields elements from multiple coroutines in sequence.
    ''' </summary>
    Public Shared Function Concat(ParamArray coroutines As Coroutine(Of T)()) As Coroutine(Of T)
        Return New Coroutine(Of T)(Function()
                                       Return Iterator Function()
                                                  For Each coro As Coroutine(Of T) In coroutines
                                                      coro.Start()
                                                      While coro.Continue()
                                                          Yield coro.Current
                                                      End While
                                                  Next coro
                                              End Function()
                                   End Function)
    End Function

    ''' <summary>
    ''' Wraps the coroutine in a delegate that can be used to start the coroutine.
    ''' </summary>
    ''' <param name="coroutineFunc">Coroutine function that returns an IEnumerable(Of T).</param>
    ''' <returns>Delegate that can be used to start the coroutine.</returns>
    Public Shared Function AsDelegate(coroutineFunc As CoroutineFunc(Of T)) As Func(Of T)
        With New Coroutine(Of T)(coroutineFunc)
            .Start()
            Return Function() If(.Continue(), .Current, Nothing)
        End With
    End Function
    
    ''' <summary>
    ''' Gets the currently running coroutine and whether it's the main coroutine.
    ''' Similar to Lua's coroutine.running().
    ''' </summary>
    ''' <returns>A tuple containing the current coroutine (or Nothing if main) and a boolean indicating if it's the main coroutine.</returns>
    Public Shared Function Running() As (coroutine As Coroutine(Of T), isMain As Boolean)
        Dim result = CoroutineManager.Running()
        Dim typedCoroutine As Coroutine(Of T) = Nothing
        
        If TypeOf result.coroutine Is Coroutine(Of T) Then
            typedCoroutine = DirectCast(result.coroutine, Coroutine(Of T))
        End If
        
        Return (typedCoroutine, result.isMain)
    End Function
    
    ''' <summary>
    ''' Checks if the specified coroutine can yield. If no coroutine is specified, checks the current one.
    ''' Similar to Lua's coroutine.isyieldable().
    ''' </summary>
    ''' <param name="coroutine">The coroutine to check (optional, defaults to current).</param>
    ''' <returns>True if the coroutine can yield, False otherwise.</returns>
    Public Shared Function IsYieldable(Optional coroutine As Coroutine(Of T) = Nothing) As Boolean
        If coroutine Is Nothing Then
            Dim current = CoroutineManager.Running().coroutine
            If TypeOf current Is Coroutine(Of T) Then
                coroutine = DirectCast(current, Coroutine(Of T))
            Else
                Return False
            End If
        End If
        
        Return CoroutineManager.IsYieldable(coroutine)
    End Function
End Class

''' <summary>
''' A simplified coroutine that handles the execution of a coroutine.
''' </summary>
Public Class Coroutine
    Inherits Coroutine(Of Object)

    ''' <summary>
    ''' Initializes a coroutine from an enumerator.
    ''' </summary>
    ''' <param name="enumerator">Enumerator to be used for the coroutine.</param>
    Public Sub New(enumerator As IEnumerator(Of Object))
        MyBase.New(enumerator)
    End Sub

    ''' <summary>
    ''' Initializes a coroutine from a collection (iterates over the collection).
    ''' </summary>
    ''' <param name="iterable">Collection of data to be iterated over.</param>
    Public Sub New(iterable As IEnumerable(Of Object))
        MyBase.New(iterable)
    End Sub

    ''' <summary>
    ''' Initializes a coroutine from a parameter array (iterates over the array).
    ''' </summary>
    ''' <param name="iterable">Parameter array of data to be iterated over.</param>
    Public Sub New(ParamArray iterable As Object())
        MyBase.New(iterable)
    End Sub

    ''' <summary>
    ''' Initializes a coroutine from a function that returns an IEnumerable(Of Object).
    ''' </summary>
    ''' <param name="coroutineFunc">Coroutine function that returns an IEnumerable(Of Object).</param>
    Public Sub New(coroutineFunc As CoroutineFunc)
        MyBase.New(coroutineFunc)
    End Sub

    ''' <summary>
    ''' Wraps the coroutine in a delegate that can be used to start the coroutine.
    ''' </summary>
    ''' <param name="coroutineFunc">Coroutine function that returns an IEnumerable(Of Object).</param>
    ''' <returns>Delegate that can be used to start the coroutine.</returns>
    Public Shared Shadows Function AsDelegate(coroutineFunc As CoroutineFunc) As Func(Of Object)
        With New Coroutine(coroutineFunc)
            .Start()
            Return Function() If(.Continue(), .Current, Nothing)
        End With
    End Function

    ''' <summary>
    ''' Gets the currently running coroutine and whether it's the main coroutine.
    ''' Similar to Lua's coroutine.running().
    ''' </summary>
    ''' <returns>A tuple containing the current coroutine (or Nothing if main), 
    ''' and a boolean indicating if it's the main coroutine.</returns>
    Public Shared Shadows Function Running() As (coroutine As Coroutine, isMain As Boolean)
        Dim result = CoroutineManager.Running()
        Dim coro As Coroutine = Nothing

        If TypeOf result.coroutine Is Coroutine Then coro = DirectCast(result.coroutine, Coroutine)

        Return (coro, result.isMain)
    End Function

    ''' <summary>
    ''' Checks if the specified coroutine can yield. If no coroutine is specified, checks 
    ''' the current one. Similar to Lua's coroutine.isyieldable().
    ''' </summary>
    ''' <param name="coroutine">The coroutine to check (optional, defaults to current).</param>
    ''' <returns>True if the coroutine can yield, False otherwise.</returns>
    Public Shared Shadows Function IsYieldable(Optional coroutine As Coroutine = Nothing) As Boolean
        If coroutine Is Nothing Then
            Dim current = CoroutineManager.Running().coroutine
            If TypeOf current Is Coroutine Then
                coroutine = DirectCast(current, Coroutine)
            Else
                Return False
            End If
        End If

        Return CoroutineManager.IsYieldable(coroutine)
    End Function
End Class
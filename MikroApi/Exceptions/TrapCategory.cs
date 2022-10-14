namespace DanilovSoft.MikroApi;

public enum TrapCategory
{
    /// <summary>
    /// Missing item or command
    /// </summary>
    MissingItemOrCommand = 0,
    /// <summary>
    /// Argument value failure
    /// </summary>
    ArgumentValueFailure = 1,
    /// <summary>
    /// Execution of command interrupted
    /// </summary>
    ExecutionOfCommandInterrupted = 2,
    /// <summary>
    /// Scripting related failure
    /// </summary>
    ScriptingRelatedFailure = 3,
    /// <summary>
    /// General failure
    /// </summary>
    GeneralFailure = 4,
    /// <summary>
    /// API related failure
    /// </summary>
    ApiRelatedFailure = 5,
    /// <summary>
    /// TTY related failure
    /// </summary>
    TtyRelatedFailure = 6,
    /// <summary>
    /// Value generated with :return command
    /// </summary>
    ValueGeneratedWithReturnCommand = 7
}

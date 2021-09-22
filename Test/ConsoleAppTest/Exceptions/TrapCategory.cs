namespace MikroApi
{
    public enum TrapCategory
    {
        Missing_item_or_command = 0,
        Argument_value_failure = 1,
        Execution_of_command_interrupted = 2,
        Scripting_related_failure = 3,
        General_failure = 4,
        API_related_failure = 5,
        TTY_related_failure = 6,
        Value_generated_with_return_command = 7
    }
}

namespace CommonTypes
{
    public class StringContainer
    {
        public string ContainedString { get; set; }
        public StringContainer() { }
        public StringContainer(string stringThatNeedContainer)
        {
            ContainedString = stringThatNeedContainer;
        }

    }
}

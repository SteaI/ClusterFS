using System;

namespace CFS.Exceptions
{
    /// <summary>
    /// 헤더 길이보다 스트림길이가 짧아 발생하는 예외입니다.
    /// </summary>
    public class HeaderNotFoundException : Exception
    {
        internal HeaderNotFoundException(string message) : base(message)
        {
            
        }
    }
}

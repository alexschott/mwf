using System.Collections.Generic;

namespace MWF.Mobile.Core
{

    public class HttpResult<T> : HttpResult
    {

        public HttpResult()
            : base()
        {
        }

        protected HttpResult(IList<string> errors)
            : base(errors)
        {
        }

        public HttpResult(T content)
            : base()
        {
            this.Content = content;
        }

        public T Content { get; set; }

        public static new HttpResult<T> Failure(string error)
        {
            return new HttpResult<T>(new[] { error });
        }

        public static new HttpResult<T> Failure(IList<string> errors)
        {
            return new HttpResult<T>(errors);
        }

    }

}

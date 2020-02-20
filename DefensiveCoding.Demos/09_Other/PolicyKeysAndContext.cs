using DefensiveCoding.Demos.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace DefensiveCoding.Demos._09_Other
{
    [TestClass]
    public class PolicyKeysAndContext
    {
        /// <summary>
        /// You can name your policies using a policy key.  
        /// This is helpful when you are using shared policies across your application
        /// </summary>
        [TestMethod]
        public void NamePolicyUsingPolicyKey()
        {
            bool retried = false;
            var dataAccessPolicy = Policy
                .Handle<DataAccessException>()
                .Retry(1, onRetry: (ex, retries, ctx) =>
                {
                    retried = true;
                    Assert.AreEqual("DataAccessRetryPolicy", ctx.PolicyKey);
                    // log information about retry here
                })
                .WithPolicyKey("DataAccessRetryPolicy");

            dataAccessPolicy.Execute(() => CallDatabase(retried));

            Assert.IsTrue(retried);
        }


        /// <summary>
        /// An operation key can provide information about the caller that used the policy
        /// </summary>
        [TestMethod]
        public void UsePolicyKeyAndOperationKey()
        {
            bool retried = false;
            var dataAccessPolicy = Policy
                .Handle<DataAccessException>()
                .Retry(1, onRetry: (ex, retries, ctx) =>
                {
                    retried = true;
                    Assert.AreEqual("DataAccessRetryPolicy", ctx.PolicyKey);
                    Assert.AreEqual(nameof(UsePolicyKeyAndOperationKey), ctx.OperationKey);
                    // log information about retry here
                })
                .WithPolicyKey("DataAccessRetryPolicy");

            dataAccessPolicy.Execute((ctx) => CallDatabase(retried), new Context(nameof(UsePolicyKeyAndOperationKey)));

            Assert.IsTrue(retried);
        }

        /// <summary>
        /// In additional to a policy and operation key, you can include whatever additional data you want
        /// </summary>
        [TestMethod]
        public void IncludeAdditionalContextData()
        {
            bool retried = false;
            var dataAccessPolicy = Policy
                .Handle<DataAccessException>()
                .Retry(1, onRetry: (ex, retries, ctx) =>
                {
                    retried = true;
                    Assert.AreEqual("DataAccessRetryPolicy", ctx.PolicyKey);
                    Assert.AreEqual(nameof(UsePolicyKeyAndOperationKey), ctx.OperationKey);
                    Assert.AreEqual("12345", ctx["CustomerId"]);
                    // log information about retry here
                })
                .WithPolicyKey("DataAccessRetryPolicy");

            dataAccessPolicy.Execute((ctx) => CallDatabase(retried), new Context(nameof(UsePolicyKeyAndOperationKey), new Dictionary<string, object>() { {"CustomerId", "12345" } }));

            Assert.IsTrue(retried);
        }

        private void CallDatabase(bool isRetry)
        {
            if (!isRetry)
            {
                throw new DataAccessException();
            }
        }        
    }

    internal class DataAccessException : Exception
    {
    }
}

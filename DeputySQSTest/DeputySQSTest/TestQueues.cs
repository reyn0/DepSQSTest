using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Deputy
{
    /// <summary>
    /// This is queue specific test
    /// </summary>
    [TestClass]
    public class TestQueues
    {
        //Setting up the commonly used variables
        AmazonSQSConfig sqsConfig = new AmazonSQSConfig();
        AmazonSQSClient sqsClient;
        private const string prefix = "TestQueue";
        private string queueName;
        private Task<string> queueURL;
        private string messageBody = "Lorem ipsum dolor sit amet, consectetur adipiscing elit";
        private string messageReceipt;

        #region Test Setup 
        /// <summary>
        /// Initialize the test and set the config to point to elasticmq
        /// </summary>
        [TestInitialize]
        public void TestInit()
        {
            // Set the service URL to point to elasticmq URL
            sqsConfig.ServiceURL = "http://localhost:9324/";

            // Create a new sqs client
            sqsClient = new AmazonSQSClient("x", "x", sqsConfig);
        }

        /// <summary>
        /// Clean up the queue after every test. 
        /// Make sure that we delete every queue created by this test run
        /// </summary>
        [TestCleanup]
        public void SQSCleanUp()
        {
            var sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            foreach (string queue in sqsQueueList.Result.QueueUrls)
            {
                if (queue.Contains(prefix))
                {
                    try
                    {
                        sqsClient.DeleteQueueAsync(queue);
                    }
                    catch (Exception)
                    {
                        Console.Write("Failed to clean up queue {0}", queue);
                    }
                }
            }
        }
        #endregion


        #region Positive Test
        /// <summary>
        /// Create the queue 
        /// Steps:
        /// 1. Check if there is something in the queue already
        /// 2. Create the queue
        /// 3. Verify that the queue is created and the list is not null
        /// 4. Verify the queue list contains the queue name created before
        /// 5. Verify the create response url and the queue url is the same
        /// </summary>
        [TestMethod]
        public async Task TestCreateQueueAsync()
        {
            queueName = setQueueName();
            // Check if any queue is already created
            var sqsQueueList = await sqsClient.ListQueuesAsync(prefix);
            Assert.IsFalse(sqsQueueList.QueueUrls.Count >= 1, "There is something in the queue already");

            // Create the queue
            CreateQueueResponse createResponse = await sqsClient.CreateQueueAsync(queueName);

            // Get the queue list. Assert is added to make sure that the queue is created and make sure that we only create 1 queue
            sqsQueueList = await sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 1, "Queue is not created or more than one queue is created");

            // Verify the queue list contains the queue name created before
            Assert.IsTrue(sqsQueueList.QueueUrls[0].Contains(queueName), "Queue name is not the same");

            // Verify the response created from when creating the queue is the same as the one we get from get queue URL
            var request = new GetQueueUrlRequest(queueName);
            var response = sqsClient.GetQueueUrlAsync(request);
            Assert.AreEqual(createResponse.QueueUrl, response.Result.QueueUrl, "The queue URL is not the same");
        }

        /// <summary>
        /// Delete the queue
        /// Steps:
        /// 1. Create the queue
        /// 2. Since we know that we only created 1 queue, we can delete the only one we created
        /// 3. Verify that the list count is now 0
        /// </summary>
        [TestMethod]
        public async Task TestDeleteQueueAsync()
        {
            // Create the queue 
            queueName = setQueueName();
            await sqsClient.CreateQueueAsync(queueName);

            // Get the list and make sure that the queue is created and make sure that we only create 1 queue
            var sqsQueueList = await sqsClient.ListQueuesAsync(queueName);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 1, "Queue is not created or more than one queue is created");

            // Since we know that we only created 1 queue, we can delete the only one
            await sqsClient.DeleteQueueAsync(sqsQueueList.QueueUrls[0].ToString());

            // Get the list again, and verify that the list count is now 0
            sqsQueueList = await sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 0, "The queue is not deleted");
        }

        /// <summary>
        /// Test Get Queue URL
        /// Steps:
        /// 1. Create the queue
        /// 2. Get queue URL
        /// </summary>
        [TestMethod]
        public void TestGetQueueURL()
        {
            // Create the queue 
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            string expectedQueueURL = "http://localhost:9324/queue/" + queueName;

            Assert.AreEqual(expectedQueueURL, queueURL.Result.ToString(), "Queue URL is not the same");
        }

        /// <summary>
        /// Purge Queue
        /// Steps:
        /// 1. Create the queue
        /// 2. Send the message
        /// 3. Purge the queue
        /// 4. Verify the message is no longer in the queue
        /// </summary>
        [TestMethod]
        public async Task TestPurgeQueueAsync()
        {
            // Create the queue
            queueName = setQueueName();
            queueURL = createQueueURLAsync(queueName);

            // Send message
            var request = new SendMessageRequest(queueURL.Result.ToString(), messageBody);
            await sqsClient.SendMessageAsync(request);
            var receiveResponse = sqsClient.ReceiveMessageAsync(queueURL.Result.ToString());
            Assert.IsTrue(receiveResponse.Result.Messages.Count == 1, "Message is not created");

            // Purge the queue
            await sqsClient.PurgeQueueAsync(queueURL.Result.ToString());

            // Verify the message is no longer in the queue
            receiveResponse = sqsClient.ReceiveMessageAsync(queueURL.Result.ToString());
            Assert.IsTrue(receiveResponse.Result.Messages.Count == 0, "Message is not deleted");
        }

        /// <summary>
        /// Lists queue
        /// Steps:
        /// 1. Create 2 queue
        /// 2. Verify there are 2 queues in the list queue
        /// </summary>
        [TestMethod]
        public async Task TestListQueueAsync()
        {
            // Create queue no 1
            queueName = setQueueName();
            await sqsClient.CreateQueueAsync(queueName);

            // Create queue no 2
            queueName = setQueueName();
            await sqsClient.CreateQueueAsync(queueName);

            // Verify there are 2 queues in the list
            var sqsQueueList = await sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.QueueUrls.Count == 2, "Message count in the list queue is not the same");
        }
        #endregion

        #region Negative test
        /// <summary>
        /// Delete non existent queue
        /// Steps:
        /// 1. Do not create any queue
        /// 2. Get the list and make sure that the queue is created and make sure that we only create 1 queue
        /// 3. Get the list again, and verify that the list count is now 0
        /// </summary>
        [TestMethod]
        public void TestDeleteNonExistQueue()
        {
            // Do not create a queue
            queueName = setQueueName();

            // Get the list and make sure that the queue is created and make sure that we only create 1 queue
            var sqsQueueList = sqsClient.ListQueuesAsync(queueName);
            Assert.IsTrue(sqsQueueList.Result.QueueUrls.Count == 0, "There is something in the list");

            // Try to delete non existent queue
            try
            {
                sqsClient.DeleteQueueAsync(sqsQueueList.Result.QueueUrls[0].ToString());
            }
            catch (ArgumentException e)
            {
                Assert.AreEqual("Index was out of range. Must be non-negative and less than the size of the collection.\r\nParameter name: index", e.Message.ToString(), "Queue is able to be deleted, which is not suppose to");
            }

            // Get the list again, and verify that the list count is now 0
            sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.Result.QueueUrls.Count == 0, "The queue still have something in it");
        }

        /// <summary>
        /// Create same queue twice
        /// Steps:
        /// 1. Create the first queue
        /// 2. Create another queue that have the same name
        /// 3. Get the list again, and verify that the list count is still 1
        /// </summary>
        [TestMethod]
        public async Task TestCreateSameQueueTwiceAsync()
        {
            // Create the queue
            queueName = setQueueName();
            CreateQueueResponse createResponse = await sqsClient.CreateQueueAsync(queueName);

            // Get the queue list. Assert is added to make sure that the queue is created and make sure that we only create 1 queue
            var sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            Assert.IsTrue(sqsQueueList.Result.QueueUrls.Count == 1, "Queue is not created or more than one queue is created");

            // Create another queue that have the same name
            createResponse = await sqsClient.CreateQueueAsync(queueName);

            // Verify there are only 1 queue created
            sqsQueueList = sqsClient.ListQueuesAsync(prefix);
            Assert.IsFalse(sqsQueueList.Result.QueueUrls.Count == 2, "Extra queue was added in the list");
        }
        #endregion

        #region Private Methods
        /// <summary>
        /// Set the  queue name with prefix and randomize at the end
        /// so that we knows that every queue is unique
        /// </summary>
        /// <returns>String queue name</returns>
        private string setQueueName()
        {
            return prefix + new Random().Next();
        }

        /// <summary>
        /// Create the queue and return the queue URL
        /// </summary>
        /// <param name="queueName"></param>
        /// <returns>String queue URL</returns>
        private async Task<string> createQueueURLAsync(string queueName)
        {
            // Create the queue
            await sqsClient.CreateQueueAsync(queueName);

            // List the queue
            var sqsQueueList = sqsClient.ListQueuesAsync(prefix);

            // Since we know that we only created 1 queue, we can say the array no 0
            return sqsQueueList.Result.QueueUrls[0].ToString();
        }

        /// <summary>
        /// Validate message body
        /// </summary>
        /// <param name="message"></param>
        /// <param name="md5"></param>
        private static void ValidateMD5(string message, string md5)
        {
            Amazon.SQS.Internal.ValidationResponseHandler.ValidateMD5(message, md5);
        }
        #endregion
    }
}
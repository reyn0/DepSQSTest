PREREQUISITE:

Before we run this, make sure to start elasticmq server by running this at command prompt:
java -jar elasticmq-server-0.14.6.jar
Then go to http://localhost:9324/ in the web browser to make sure the server is run
Once you finished with the test, you can turn off the command prompt by Ctrl+C

=================================================================================================

HOW TO RUN:

The test can be run from Visual Studio
- Open the Deputy.sln with Visual Studio
- Build Solution from Visual Studio
- Run the tests from test explorer in Visual Studio

=================================================================================================

Technology:
I use C#

Why:
I needed to use language that I am familiar with and something that can be easily build and tested

Technical details:
- I was having a bit difficulty at first because I don't have my environment set up properly.
I needed to install Visual Studio, JRE and JDK in my machine and that tooks a while to set up.
- After finished installing all of that, I started with a simple create queue test as that is
the most important in AWS. Without the queue, we cannot manipulate the messages
- I then started working on Delete Queue, Get Queue URL, Send Message, Retrieve Message and Delete Message. 


What can be tested further:
If I had more time to implement this, I would like to expand the test further about:
- Batch sending message and delete message
- Some extended functionality like: Change Message Visibility, Get Queue Attributes, Dead Letter Queue, Permissions, etc
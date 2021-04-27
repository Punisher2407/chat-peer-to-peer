namespace P2PChat.Clients_Message
{
    // Own message format, allows you to send messages of different types and simplify
    // the transmission of messages in the streaming mode used in TCP.
    public class Message
    {
        public char code { get; }
        public string data { get; }
        // byte-information
        public const char CONNECT = '0'; // 0 byte - connect
        public const char MESSAGE = '1'; // 1 byte - message
        public const char DISCONNECT = '2'; // 2 byte - disconnect
        public const char GET_HISTORY = '3'; // 3 byte - get chat history
        public const char SHOW_HISTORY = '4'; // 4 byte - send chat history for new user
        public Message(char messageCode, string messageData)
        {
            code = messageCode;
            data = messageData;
        }
    }
}

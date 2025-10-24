using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Configuration;
using System.Text;

namespace ClientRabbit
{
    public partial class Form1 : Form
    {
        private readonly string _clientName;
        private readonly string _responseQueueName;
        private bool isProcessingResponse = false;
        private IModel _channel;
        private IConnection _connection;

        public Form1()
        {
            InitializeComponent();
            _clientName = ConfigurationManager.AppSettings["ClientName"] ?? $"Default Client_{Guid.NewGuid()}";
            _responseQueueName = $"response_queue_{_clientName}";
            InitializeRabbitMQ();
        }

        private void InitializeRabbitMQ()
        {	
			var factory = new ConnectionFactory()
			{
				HostName = "<YOUR_RABBITMQ_HOST>",
				Port = 5672,
				UserName = "<YOUR_RABBITMQ_USERNAME>",
				Password = "<YOUR_RABBITMQ_PASSWORD>"
			};

            try
            {
                _connection = factory.CreateConnection();
                _channel = _connection.CreateModel();

                _channel.QueueDeclare(queue: "request_queue", durable: false, exclusive: false, autoDelete: false, arguments: null);
                _channel.QueueDeclare(queue: _responseQueueName, durable: false, exclusive: true, autoDelete: true, arguments: null);

                var consumer = new EventingBasicConsumer(_channel);
                consumer.Received += (sender, e) =>
                {
                    var body = e.Body.ToArray();
                    var response = Encoding.UTF8.GetString(body);

                    if (isProcessingResponse)
                    {
                        return;
                    }

                    isProcessingResponse = true;
                    HandleResponse(response);
                };

                _channel.BasicConsume(queue: _responseQueueName, autoAck: true, consumer: consumer);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка инициализации RabbitMQ: {ex.Message}");
            }
        }

        private async void btnRequest_Click(object sender, EventArgs e)
        {
            await SendRequest();
        }

        private async Task SendRequest()
        {
            try
            {
                string message = $"Запрос от {_clientName}";
                var body = Encoding.UTF8.GetBytes(message);

                var properties = _channel.CreateBasicProperties();
                properties.ReplyTo = _responseQueueName;

                _channel.BasicPublish(exchange: "", routingKey: "request_queue", basicProperties: properties, body: body);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка отправки запроса: {ex.Message}");
            }
        }

        private void HandleResponse(string response)
        {
            string timeEnter = "Error";
            string data = "Error";
            int sizeData = 0;
            string status = "Error";

            if (!string.IsNullOrEmpty(response))
            {
                var outputParts = response.Split(' ');
                timeEnter = outputParts[0] + " " + outputParts[1];
                data = outputParts[2];
                sizeData = int.Parse(outputParts[3]);
                status = outputParts[4];
            }
            else
            {
                MessageBox.Show("Ошибка: Вывод пуст.");
            }

            this.Invoke((Action)(() =>
            {
                label1.Text = "Ответ от сервера: " + "\n" + "Время: " + timeEnter + "\n" + "Размер: " + sizeData + "\n" + "Статус: " + status;
            }));

            Decoder.DecodeErrorDataAndShowMessage(data);

            isProcessingResponse = false;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _channel?.Close();
            _connection?.Close();
            base.OnFormClosing(e);
        }
    }
}

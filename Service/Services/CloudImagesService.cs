using Grpc.Core;
using Services.CloudService;


namespace Service.Services
{
    public class CloudImagesService : CloudService.CloudServiceBase
    {
        public override async Task<Bool> Upload(IAsyncStreamReader<ImageData> requestStream, ServerCallContext context)
        {
            await requestStream.MoveNext();
            if(requestStream.Current.FileSize == 0)
            {
                return new Bool() { Success = false };
            }
            int size = requestStream.Current.FileSize;
            string filename;
            if (string.IsNullOrEmpty(requestStream.Current.FileName))
                filename = Guid.NewGuid().ToString();
            else
                filename = requestStream.Current.FileName;
            Directory.CreateDirectory("data");
            FileStream fs = File.OpenWrite($"data/{filename}");
            do
            {
                await requestStream.MoveNext();
                byte[] data = requestStream.Current.Data.ToArray();
                fs.Write(data, 0, data.Length);
            }
            while (fs.Position != size);
            fs.Close();
            return new Bool() { Success = true };
        }

        public override async Task Download(DownloadImageRequest request, IServerStreamWriter<ImageData> responseStream, ServerCallContext context)
        {
            if (string.IsNullOrEmpty(request.FileName))
                return;
            string[] files = Directory.GetFiles("data");
            if(!files.Contains($"data\\{request.FileName}"))
                return;
            FileStream fs = File.OpenRead($"data/{request.FileName}");
            await responseStream.WriteAsync(new ImageData() { FileName = request.FileName, FileSize = (int)fs.Length });
            do
            {
                long readLength = fs.Length - fs.Position >= 512 ? 512 : fs.Length - fs.Position;
                byte[] data = new byte[readLength];
                fs.Read(data, 0, (int)readLength);            
                await responseStream.WriteAsync(new ImageData() { Data = Google.Protobuf.ByteString.CopyFrom(data) });
            } while (fs.Position != fs.Length);
            return;
        }

        public override async Task ListFiles(Empty request, IServerStreamWriter<ImagesList> responseStream, ServerCallContext context)
        {
            do
            {
                ImagesList list = new ImagesList();
                list.FileNames.AddRange(Directory.GetFiles("data"));
                await responseStream.WriteAsync(list);
                Thread.Sleep(5000);
            } while (!context.CancellationToken.IsCancellationRequested);
        }
    }
}

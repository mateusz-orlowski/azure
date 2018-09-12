using Microsoft.ProjectOxford.Face;
using Microsoft.ProjectOxford.Face.Contract;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FaceAPI = Microsoft.ProjectOxford.Face;

namespace LiveCameraSample
{
    class FaceRecognition
    {
        private FaceAPI.FaceServiceClient _faceClient = null;
        private const string _groupId = "atosemployees";

        /// <summary> Function which submits a frame to the Face API. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the faces returned by the API. </returns>
        public async Task UploadFaces()
        {
            _faceClient = new FaceAPI.FaceServiceClient(Properties.Settings.Default.FaceAPIKey, Properties.Settings.Default.FaceAPIHost);
            var groups = await _faceClient.ListPersonGroupsAsync();
            var exists = false;
            foreach (var group in groups)
            {
                if (group.PersonGroupId.Equals(_groupId))
                {
                    exists = true;
                    break;
                }
            }
            if (exists)
            {
                var persons = await _faceClient.ListPersonsAsync(_groupId);
                foreach (var person in persons)
                {
                    await _faceClient.DeletePersonAsync(_groupId, person.PersonId);
                }
            }
            else
            {
                await _faceClient.CreatePersonGroupAsync(_groupId, "AtosEmployees");
            }

            var iterator = 0;
            foreach (string imagePath in Directory.GetFiles("C:\\Temp\\Pics"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    try
                    {
                        CreatePersonResult person = await _faceClient.CreatePersonAsync(_groupId, "Person " + iterator++);
                        await Task.Delay(5000);
                        Properties.Settings.Default.UploadStatus = String.Format("Uploading {0}: {1}", iterator, imagePath);
                        
                        await _faceClient.AddPersonFaceAsync(_groupId, person.PersonId, s);
                    }
                    catch (FaceAPIException fae)
                    {
                        var finishedAt = "";
                    }
                    await Task.Delay(5000);
                }
            }
        }

        /// <summary> Function which submits a frame to the Face API. </summary>
        /// <param name="frame"> The video frame to submit. </param>
        /// <returns> A <see cref="Task{LiveCameraResult}"/> representing the asynchronous API call,
        ///     and containing the faces returned by the API. </returns>
        public async Task UpdatePerson(string personId)
        {
            _faceClient = new FaceAPI.FaceServiceClient(Properties.Settings.Default.FaceAPIKey, Properties.Settings.Default.FaceAPIHost);
            var groups = await _faceClient.ListPersonGroupsAsync();

            foreach (string imagePath in Directory.GetFiles("C:\\Temp\\Pics\\Mateusz"))
            {
                using (Stream s = File.OpenRead(imagePath))
                {
                    try
                    {
                        var persons = await _faceClient.ListPersonsAsync(_groupId);
                        Person person = persons.Where(pers => pers.Name == personId).FirstOrDefault();
                        Properties.Settings.Default.UploadStatus = String.Format("Updating {0} face: {1}", person.Name, imagePath);

                        await _faceClient.AddPersonFaceAsync(_groupId, person.PersonId, s);
                    }
                    catch (FaceAPIException fae)
                    {
                        Properties.Settings.Default.UploadStatus = "Error: " + fae.Message;
                    }
                }
            }
        }

        public async Task<Boolean> StartTraining()
        {
            _faceClient = new FaceAPI.FaceServiceClient(Properties.Settings.Default.FaceAPIKey, Properties.Settings.Default.FaceAPIHost);
            await _faceClient.TrainPersonGroupAsync(_groupId);

            TrainingStatus trainingStatus = null;
            while (true)
            {
                trainingStatus = await _faceClient.GetPersonGroupTrainingStatusAsync(_groupId);

                if (trainingStatus.Status != Status.Running)
                {
                    break;
                }

                await Task.Delay(1000);
            }

            var persons = await _faceClient.ListPersonsAsync(_groupId);

            return true;
        }

        public async Task<Person> Identify(FaceServiceClient _faceClient, Face[] faces)
        {
            var faceIds = faces.Select(face => face.FaceId).ToArray();
            Properties.Settings.Default.FaceAPICallCount++;
            if (faceIds.Length > 0)
            {
                foreach (var identifyResult in await _faceClient.IdentifyAsync(_groupId, faceIds))
                {
                    if (identifyResult.Candidates.Length != 0)
                    {
                        var candidateId = identifyResult.Candidates[0].PersonId;
                        var person = await _faceClient.GetPersonAsync(_groupId, candidateId);
                        Properties.Settings.Default.FaceAPICallCount++;
                        return person;
                        // user identificated: person.name is the associated name
                    }
                    else
                    {
                        // user not recognized
                    }
                }
            }
            return null;
        }
    }
}

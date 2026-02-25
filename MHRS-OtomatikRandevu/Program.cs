using MHRS_OtomatikRandevu.Models;
using MHRS_OtomatikRandevu.Models.RequestModels;
using MHRS_OtomatikRandevu.Models.ResponseModels;
using MHRS_OtomatikRandevu.Services;
using MHRS_OtomatikRandevu.Services.Abstracts;
using MHRS_OtomatikRandevu.Urls;
using MHRS_OtomatikRandevu.Utils;
using System.Net;

namespace MHRS_OtomatikRandevu
{
    public class Program
    {
        static string TC_NO;
        static string SIFRE;

        const string TOKEN_FILE_NAME = "token.txt";
        static string JWT_TOKEN;
        static DateTime TOKEN_END_DATE;

        static IClientService _client;
        static INotificationService _notificationService;

        static void Main(string[] args)
        {
            _client = new ClientService();
            _notificationService = new NotificationService();

            #region Giriş Yap Bölümü
            string jwtToken = null;
            while (string.IsNullOrWhiteSpace(jwtToken))
            {
                Console.WriteLine("Lütfen MHRS JWT token'ınızı girin:");
                jwtToken = Console.ReadLine()?.Trim();
            }
            JWT_TOKEN = jwtToken;
            TOKEN_END_DATE = JwtTokenUtil.GetTokenExpireTime(JWT_TOKEN);
            _client.AddOrUpdateAuthorizationHeader(jwtToken);

            // Token doğrulama için örnek bir istek atabilirsiniz:
            if (!_client.ValidateToken())
            {
                Console.WriteLine("Token doğrulaması başarısız veya servis erişilemez. Devam etmek için token'ınızı doğrulayın.");
                return;
            }
            #endregion

            #region İl Seçim Bölümü
            int provinceIndex;
            var provinceListResponse = _client.GetSimple<List<GenericResponseModel>>(MHRSUrls.BaseUrl, MHRSUrls.GetProvinces);
            if (provinceListResponse == null || !provinceListResponse.Any())
            {
                Console.WriteLine("Bir hata meydana geldi!");
                Thread.Sleep(2000);
                return;
            }
            var provinceList = provinceListResponse
                                    .DistinctBy(x => x.Value)
                                    .OrderBy(x => x.Value)
                                    .ToList();
            var istanbulSubLocationIds = new int[] { 341, 342 };
            do
            {
                Console.Clear();
                Console.WriteLine("-------------------------------------------");
                for (int i = 0; i < provinceList.Count; i++)
                {
                    Console.WriteLine($"{i + 1}-{provinceList[i].Text}");
                }
                Console.WriteLine("-------------------------------------------");
                Console.Write("İl Numarası (Plaka) Giriniz: ");
                provinceIndex = Convert.ToInt32(Console.ReadLine());

                if (provinceIndex == 34)
                {
                    int subLocationIndex;
                    do
                    {
                        Console.Clear();
                        Console.WriteLine("-------------------------------------------");
                        Console.WriteLine($"0-İSTANBUL\n1-İSTANBUL (AVRUPA)\n2-İSTANBUL (ANADOLU)");
                        Console.WriteLine("-------------------------------------------");

                        Console.Write(@"Alt Bölge Numarası Giriniz: ");
                        subLocationIndex = Convert.ToInt32(Console.ReadLine()); ;
                    } while (subLocationIndex < 0 && subLocationIndex > 2);

                    if (subLocationIndex != 0)
                        provinceIndex = int.Parse("34" + subLocationIndex);
                }

            } while ((provinceIndex < 1 || provinceIndex > 81) && !istanbulSubLocationIds.Contains(provinceIndex));

            #endregion

            #region İlçe Seçim Bölümü
            int districtIndex;
            var districtList = _client.GetSimple<List<GenericResponseModel>>(MHRSUrls.BaseUrl, string.Format(MHRSUrls.GetDistricts, provinceIndex));
            if (districtList == null || !districtList.Any())
            {
                ConsoleUtil.WriteText("Bir hata meydana geldi!", 2000);
                return;
            }

            do
            {
                Console.Clear();
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("0-FARKETMEZ");
                for (int i = 0; i < districtList.Count; i++)
                {
                    Console.WriteLine($"{i + 1}-{districtList[i].Text}");
                }
                Console.WriteLine("-------------------------------------------");
                Console.Write("İlçe Numarası Giriniz: ");
                districtIndex = Convert.ToInt32(Console.ReadLine()); ;

            } while (districtIndex < 0 || districtIndex > districtList.Count);

            if (districtIndex != 0)
                districtIndex = districtList[districtIndex - 1].Value;
            else
                districtIndex = -1;
            #endregion

            #region Klinik Seçim Bölümü
            int clinicIndex;
            var clinicListResponse = _client.Get<List<GenericResponseModel>>(MHRSUrls.BaseUrl, string.Format(MHRSUrls.GetClinics, provinceIndex, districtIndex));
            if (!clinicListResponse.Success && (clinicListResponse.Data == null || !clinicListResponse.Data.Any()))
            {
                ConsoleUtil.WriteText("Bir hata meydana geldi!", 2000);
                return;
            }
            var clinicList = clinicListResponse.Data;
            do
            {
                Console.Clear();
                Console.WriteLine("-------------------------------------------");
                for (int i = 0; i < clinicList.Count; i++)
                {
                    Console.WriteLine($"{i + 1}-{clinicList[i].Text}");
                }
                Console.WriteLine("-------------------------------------------");
                Console.Write("Klinik Numarası Giriniz: ");
                clinicIndex = Convert.ToInt32(Console.ReadLine()); ;

            } while (clinicIndex < 1 || clinicIndex > clinicList.Count);
            clinicIndex = clinicList[clinicIndex - 1].Value;
            #endregion

            #region Hastane Seçim Bölümü
            int hospitalIndex;
            var hospitalListResponse = _client.Get<List<GenericResponseModel>>(MHRSUrls.BaseUrl, string.Format(MHRSUrls.GetHospitals, provinceIndex, districtIndex, clinicIndex));
            if (!hospitalListResponse.Success && (hospitalListResponse.Data == null || !hospitalListResponse.Data.Any()))
            {
                ConsoleUtil.WriteText("Bir hata meydana geldi!", 2000);
                return;
            }
            var hospitalList = hospitalListResponse.Data;
            do
            {
                Console.Clear();
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("0-FARKETMEZ");
                for (int i = 0; i < hospitalList.Count; i++)
                {
                    Console.WriteLine($"{i + 1}-{hospitalList[i].Text}");
                }
                Console.WriteLine("-------------------------------------------");
                Console.Write("Hastane Numarası Giriniz: ");
                hospitalIndex = Convert.ToInt32(Console.ReadLine()); ;
            } while (hospitalIndex < 0 || hospitalIndex > hospitalList.Count);

            if (hospitalIndex != 0)
            {
                var hospital = hospitalList[hospitalIndex - 1];
                if (hospital.Children.Any())
                {
                    do
                    {
                        Console.Clear();
                        Console.WriteLine("-------------------------------------------");
                        Console.WriteLine($"0-{hospital.Text}");
                        for (int i = 0; i < hospital.Children.Count; i++)
                        {
                            Console.WriteLine($"{i + 1}-{hospital.Children[i].Text}");
                        }
                        Console.WriteLine("-------------------------------------------");
                        Console.Write("Hastane/Poliklinik Numarası Giriniz: ");
                        hospitalIndex = Convert.ToInt32(Console.ReadLine()); ;
                    } while (0 < hospitalIndex || hospitalIndex > hospital.Children.Count);

                    if (hospitalIndex == 0)
                        hospitalIndex = hospital.Value;
                    else
                        hospitalIndex = hospital.Children[hospitalIndex - 1].Value;
                }
                else
                {
                    hospitalIndex = hospitalList[hospitalIndex - 1].Value;
                }

            }
            else
            {
                hospitalIndex = -1;
            }

            #endregion

            #region Muayene Yeri Seçim Bölümü
            int placeIndex;
            var placeListResponse = _client.Get<List<ClinicResponseModel>>(MHRSUrls.BaseUrl, string.Format(MHRSUrls.GetPlaces, hospitalIndex, clinicIndex));
            if (!placeListResponse.Success && (placeListResponse.Data == null || !placeListResponse.Data.Any()))
            {
                ConsoleUtil.WriteText("Bir hata meydana geldi!", 2000);
                return;
            }
            var placeList = placeListResponse.Data;

            do
            {
                Console.Clear();
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("0-FARKETMEZ");
                for (int i = 0; i < placeList.Count; i++)
                {
                    Console.WriteLine($"{i + 1}-{placeList[i].Text}");
                }
                Console.WriteLine("-------------------------------------------");
                Console.Write("Muayene Yeri Numarası Giriniz: ");
                placeIndex = Convert.ToInt32(Console.ReadLine()); ;
            } while (placeIndex < 0 || placeIndex > placeList.Count);

            if (placeIndex != 0)
                placeIndex = placeList[placeIndex - 1].Value;
            else
                placeIndex = -1;

            #endregion

            #region Doktor Seçim Bölümü
            int doctorIndex;
            var doctorListResponse = _client.Get<List<GenericResponseModel>>(MHRSUrls.BaseUrl, string.Format(MHRSUrls.GetDoctors, hospitalIndex, clinicIndex));
            if (!doctorListResponse.Success && (doctorListResponse.Data == null || !doctorListResponse.Data.Any()))
            {
                ConsoleUtil.WriteText("Bir hata meydana geldi!", 2000);
                return;
            }
            var doctorList = doctorListResponse.Data;
            do
            {
                Console.Clear();
                Console.WriteLine("-------------------------------------------");
                Console.WriteLine("0-FARKETMEZ");
                for (int i = 0; i < doctorList.Count; i++)
                {
                    Console.WriteLine($"{i + 1}-{doctorList[i].Text}");
                }
                Console.WriteLine("-------------------------------------------");
                Console.Write("Doktor Numarası Giriniz: ");
                doctorIndex = Convert.ToInt32(Console.ReadLine()); ;
            } while (doctorIndex < 0 || doctorIndex > doctorList.Count);

            if (doctorIndex != 0)
                doctorIndex = doctorList[doctorIndex - 1].Value;
            else
                doctorIndex = -1;

            Console.Clear();
            #endregion

            #region Tarih Seçim Bölümü
            string? startDate;
            string? endDate;

            ConsoleUtil.WriteText("Tarih girmek istemiyorsanız boş bırakınız...", 0);
            ConsoleUtil.WriteText($"UYARI: Bitiş tarihi en fazla {DateTime.Now.AddDays(12).ToString("dd-MM-yyyy")} olabilir.\n", 0);

            do
            {
                Console.Write("Başlangıç tarihi giriniz (Format: Gün-Ay-Yıl): ");
                startDate = Console.ReadLine();
                if (string.IsNullOrEmpty(startDate))
                {
                    startDate = startDate != string.Empty ? startDate : null;
                    break;
                }

                try
                {
                    var dateArr = startDate.Split('-').Select(x => Convert.ToInt32(x)).ToArray();
                    var date = new DateTime(dateArr[2], dateArr[1], dateArr[0]);
                    startDate = date.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                }
                catch (Exception)
                {
                    ConsoleUtil.WriteText("Geçersiz tarih, tekrar giriniz", 0);
                }

            } while (true);


            do
            {
                Console.Write("Bitiş tarihi giriniz (Format: Gün-Ay-Yıl): ");
                endDate = Console.ReadLine();
                if (string.IsNullOrEmpty(endDate))
                {
                    endDate = endDate != string.Empty ? endDate : null;
                    break;
                }

                try
                {
                    var dateArr = endDate.Split('-').Select(x => Convert.ToInt32(x)).ToArray();
                    var date = new DateTime(dateArr[2], dateArr[1], dateArr[0]);
                    endDate = date.ToString("yyyy-MM-dd HH:mm:ss");
                    break;
                }
                catch (Exception)
                {
                    ConsoleUtil.WriteText("Geçersiz tarih, tekrar giriniz", 0);
                }

            } while (true);
            #endregion

            #region Randevu Alım Bölümü
            ConsoleUtil.WriteText("Yapmış olduğunuz seçimler doğrultusunda müsait olan ilk randevu otomatik olarak alınacaktır.", 3000);
            Console.Clear();

            bool appointmentState = false;
            bool isNotified = false;
            do
            {
                if (TOKEN_END_DATE == default || TOKEN_END_DATE < DateTime.Now)
                {
                    var tokenData = GetToken(_client);
                    if (tokenData == null || string.IsNullOrEmpty(tokenData.Token))
                    {
                        ConsoleUtil.WriteText("Yeniden giriş yapılırken bir hata meydana geldi!", 2000);
                        return;
                    }
                    JWT_TOKEN = tokenData.Token;
                    _client.AddOrUpdateAuthorizationHeader(JWT_TOKEN);
                }

                var slotRequestModel = new SlotRequestModel
                {
                    MhrsHekimId = doctorIndex,
                    MhrsIlId = provinceIndex,
                    MhrsIlceId = districtIndex,
                    MhrsKlinikId = clinicIndex,
                    MhrsKurumId = hospitalIndex,
                    MuayeneYeriId = placeIndex,
                    BaslangicZamani = startDate,
                    BitisZamani = endDate
                };

                var slot = GetSlot(_client, slotRequestModel);
                if (slot == null || slot == default)
                {
                    Console.WriteLine($"Müsait randevu bulunamadı | Kontrol Saati: {DateTime.Now.ToShortTimeString()}");
                    Thread.Sleep(TimeSpan.FromMinutes(1));
                    continue;
                }

                var appointmentRequestModel = new AppointmentRequestModel
                {
                    FkSlotId = slot.Id,
                    FkCetvelId = slot.FkCetvelId,
                    MuayeneYeriId = slot.MuayeneYeriId,
                    BaslangicZamani = slot.BaslangicZamani,
                    BitisZamani = slot.BitisZamani
                };

                Console.WriteLine($"Randevu bulundu - Müsait Tarih: {slot.BaslangicZamani}");
                appointmentState = MakeAppointment(_client, appointmentRequestModel, sendNotification: false);
            } while (!appointmentState);
            #endregion

            Console.ReadKey();
        }

        static JwtTokenModel GetToken(IClientService client)
        {
            var rawPath = string.Empty;
            var tokenFilePath = string.Empty;
            try
            {
                rawPath = Directory.GetCurrentDirectory()
                    .Split("\\bin\\")
                    .SkipLast(1)
                    .FirstOrDefault();
                tokenFilePath = Path.Combine(rawPath, TOKEN_FILE_NAME);

                if (File.Exists(tokenFilePath))
                {
                    var tokenData = File.ReadAllText(tokenFilePath);
                    if (!string.IsNullOrEmpty(tokenData) && JwtTokenUtil.GetTokenExpireTime(tokenData) > DateTime.Now)
                        return new() { Token = tokenData, Expiration = JwtTokenUtil.GetTokenExpireTime(tokenData) };
                }

                // Eğer token dosyası yoksa veya süresi dolduysa kullanıcıdan yeni token iste
                Console.WriteLine("Lütfen yeni MHRS JWT token'ınızı girin:");
                var newToken = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(newToken))
                {
                    Console.WriteLine("Geçersiz token girdiniz.");
                    return null;
                }

                // Dosya yolunu kullanabiliyorsak kaydet
                try
                {
                    if (!string.IsNullOrEmpty(tokenFilePath))
                        File.WriteAllText(tokenFilePath, newToken);
                }
                catch { }

                return new() { Token = newToken, Expiration = JwtTokenUtil.GetTokenExpireTime(newToken) };
            }
            catch (Exception)
            {
                Console.WriteLine("Giriş yapılırken bir hata meydana geldi!");
                Thread.Sleep(2000);
                return null;
            }
        }

        //Aynı gün içerisinde tek slot mevcut ise o slotu bulur
        //Aynı gün içerisinde birden fazla slot mevcut ise en yakın saati getirmez fakat en yakın güne ait bir slot getirir
        static SubSlot GetSlot(IClientService client, SlotRequestModel slotRequestModel)
        {
            var slotListResponse = client.Post<List<SlotResponseModel>>(MHRSUrls.BaseUrl, MHRSUrls.GetSlots, slotRequestModel).Result;
            if (slotListResponse.Data is null)
            {
                ConsoleUtil.WriteText("Bir hata meydana geldi!", 2000);
                return null;
            }

            var saatSlotList = slotListResponse.Data.FirstOrDefault()?.HekimSlotList.FirstOrDefault()?.MuayeneYeriSlotList.FirstOrDefault()?.SaatSlotList;
            if (saatSlotList == null || !saatSlotList.Any())
                return null;

            var slot = saatSlotList.FirstOrDefault(x => x.Bos)?.SlotList.FirstOrDefault(x => x.Bos)?.SubSlot;
            if (slot == default)
                return null;

            return slot;
        }

        static bool MakeAppointment(IClientService client, AppointmentRequestModel appointmentRequestModel, bool sendNotification)
        {
            var randevuResp = client.PostSimple(MHRSUrls.BaseUrl, MHRSUrls.MakeAppointment, appointmentRequestModel);
            if (randevuResp.StatusCode != HttpStatusCode.OK)
            {
                Console.WriteLine($"Randevu alırken bir problem ile karşılaşıldı! \nRandevu Tarihi -> {appointmentRequestModel.BaslangicZamani}");
                return false;
            }

            var message = $"Randevu alındı! \nRandevu Tarihi -> {appointmentRequestModel.BaslangicZamani}";
            Console.WriteLine(message);

            if (sendNotification)
                _notificationService.SendNotification(message).Wait();

            return true;
        }
    }
}

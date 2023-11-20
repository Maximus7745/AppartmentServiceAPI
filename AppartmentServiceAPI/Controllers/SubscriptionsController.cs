using ApartmentServiceAPI.Models;
using AppartmentServiceAPI.Models;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace ApartmentServiceAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class SubscriptionsController : Controller
    {
        ApplicationDbCobtext db;
        public SubscriptionsController(ApplicationDbCobtext context)
        {
            db = context;
        }

        /// <summary>
        /// Добавляет новую подписку по ссылке и электронной почте
        /// </summary>
        /// <param name="link">Ссылка на квартиру</param>
        /// <param name="email">Адрес электронной почты</param>
        /// <response code="200">Подписка была успешно оформлена!</response>
        /// <response code="400">Не удалось оформить подписку</response>
        [HttpPost("subscribe")]
        public async Task<IActionResult> SubscribeAsync(string link, string email)
        {
            try
            {
                if (!(CheckEmail(email) && CheckLink(link)))
                {
                    return BadRequest("Некорректная ссылка или почта!");
                }
                if (await db.Subscriptions.AnyAsync((subsc) => subsc.Email == email && subsc.Link == link))
                {
                    return BadRequest("Вы уже подписаны!");
                }
                db.Subscriptions.Add(new Subscription() { Email = email, Link = link });
                await db.SaveChangesAsync();

                return Ok("Подписка была успешно оформлена!");
            }
            catch 
            {

                return BadRequest("Не удалось оформить подписку");
            }
        }
        /// <summary>
        /// Возвращает подписки по адресу электронной почты
        /// </summary>
        /// <param name="email">Адрес электронной почты</param>
        /// <response code="200">Подписки успешно получены!</response>
        /// <response code="400">Не удалось получить ваши подписки</response>
        [HttpGet("getsubscribes")]
        public async Task<IActionResult> GetSubscribesAsync(string email)
        {
            try
            {
                var results = await db.Subscriptions.Where((subsc) => subsc.Email == email).ToListAsync();
                if (results.Count == 0)
                {
                    return BadRequest("Подписки отсутствуют");
                }
                var subscriptions = new List<SubscriptionResult>();
                foreach (var item in results)
                {
                    var subscription = new SubscriptionResult() { Link = item.Link };
                    try
                    {
                        string newPrice = ParseHtml(await GetResponseAsync(item.Link), item.Link);
                        subscription.Price = newPrice;
                    }
                    catch
                    {
                        subscription.Price = "Нет данных";
                    }
                    finally
                    {
                        subscriptions.Add(subscription);
                    }
                }
                return Ok(subscriptions);
            }
            catch (Exception)
            {

                return BadRequest("Не удалось получить ваши подписки");
            }
           
        }

        /// <summary>
        /// Возвращает все подписки
        /// </summary>
        /// <response code="200">Подписки успешно получены!</response>
        /// <response code="400">Не удалось получить подписки</response>
        [HttpGet("getallsubscribes")]
        public async Task<IActionResult> GetAllSubscribesAsync()
        {
            try
            {
                var results = await db.Subscriptions.ToListAsync();
                foreach (var item in results)
                {
                    try
                    {
                        string newPrice = ParseHtml(await GetResponseAsync(item.Link), item.Link);
                        item.Price = newPrice;
                    }
                    catch
                    {
                        item.Price = "Нет данных";
                    }

                }
                return Ok(results);
            }
            catch (Exception)
            {

                return BadRequest("Не удалось получить список подписок");
            }

        }

        private async Task<string> GetResponseAsync(string link)
        {
            var httpClient = new HttpClient();
            var response = await httpClient.GetStringAsync(link);
            httpClient.Dispose();
            return response;
        }
        private string ParseHtml(string html, string link)
        {
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            string price = "";
            string id = link.Split('/').Last();
            try
            {
                if (link.Contains("apartments"))
                {
                    var card = htmlDocument.DocumentNode.SelectSingleNode($"//div[contains(@data-id-flat, '{id}')]");
                    price = card.SelectSingleNode(".//div[contains(@class, 'card-flat__price-current')]").InnerText;
                }
                else
                {
                    var card = htmlDocument.DocumentNode.SelectSingleNode($"//a[contains(@data-house, '{id}')]");
                    price = card.SelectSingleNode(".//span[contains(@class, 'flat-link-body__price')]").InnerText;

                }
                price = Regex.Replace(price, @"<[^>]+>|&nbsp;", " ").Trim();
                return price;
            }
            catch 
            {

                return "Нет данных";
            }
        }
        /// <summary>
        /// Удаляет подписку по ссылке и электронной почте
        /// </summary>
        /// <param name="link">Ссылка на квартиру</param>
        /// <param name="email">Адрес электронной почты</param>
        /// <response code="200">Подписка была успешно удалена!</response>
        /// <response code="400">Не удалось удалить подписку</response>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteSubscribeAsync(string link, string email)
        {
            try
            {
                var subscriptions = await db.Subscriptions.Where((subsc) => subsc.Email == email && subsc.Link == link).ToArrayAsync();
                if (subscriptions.Length > 0)
                {
                    var subscription = subscriptions[0];
                    db.Subscriptions.Remove(subscription);
                    await db.SaveChangesAsync();
                    return Ok("Подписка была успешно удалена!");
                }

                return BadRequest("Данной подписки не существует!");
            }
            catch (Exception)
            {

                return BadRequest("Не удалось удалить подписку");
            }

        }

        private bool CheckEmail(string email)
        {
            try
            {
                string emailTrim = email.Trim();
                if (emailTrim.EndsWith('.'))
                {
                    return false;
                }
                MailAddress mailAddress = new MailAddress(emailTrim);
                return mailAddress.Address == emailTrim;
            }
            catch
            {
                return false;
            }
        }
        private bool CheckLink(string link)
        {
            if (link.StartsWith("https://prinzip.su/") && Uri.IsWellFormedUriString(link, UriKind.Absolute) && CheckResponseAsync(link).Result)
            {
                return true;
            }
            return false;
        }
        private async Task<bool> CheckResponseAsync(string link)
        {
            var httpClient = new HttpClient();
            try
            {
                var response = await httpClient.GetStringAsync(link);
                httpClient.Dispose();
                return true;
            }
            catch
            {
                httpClient.Dispose();
                return false;
            }
        }
    }
}

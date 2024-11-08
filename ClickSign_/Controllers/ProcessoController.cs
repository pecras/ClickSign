using Microsoft.AspNetCore.Mvc;
using SignInClick.DTOS;
using SignInClick.Services;
using System.Threading.Tasks;

namespace SignInClick.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProcessoController : ControllerBase
    {
        private readonly ProcessClickSign _processClickSign;

        public ProcessoController(ProcessClickSign processClickSign)
        {
            _processClickSign = processClickSign;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessEnvelope(IFormFile file ,string name, string signerOne , string emailOne , string signerTwo,string emailTwo)
        {
            if (name == null)
            {
                return BadRequest("Os dados do envelope não foram fornecidos.");
            }

            if (file == null)
            {
                return BadRequest("File é Obrigatório.");
            }
       
            var envelopeId = await _processClickSign.CreateEnvelopeAsync(name);


            if (envelopeId.Contains("Erro"))
            {
                return BadRequest(new { error = envelopeId });
            }


           


            var Document = await _processClickSign.ProcessDocument(envelopeId,file);

            if (Document.Contains("Erro"))
            {
                return BadRequest(new { error = Document });
            }

           var signatureOne= await _processClickSign.CreateSigner(envelopeId,signerOne,emailOne);


            if (signatureOne.Contains("Erro"))
            {
                return BadRequest(new { error = signatureOne });
            }

            var signatureTwo= await _processClickSign.CreateSigner(envelopeId,signerTwo,emailTwo);


            if (signatureTwo.Contains("Erro"))
            {
                return BadRequest(new { error = signatureTwo });
            }



            var createrequisito1 = await _processClickSign.CreateBothRequisitos(envelopeId,Document,signatureOne,signatureTwo);


            if (createrequisito1.Contains("Erro"))
            {
                return BadRequest(new { error = createrequisito1 });
            }



            var AgreeRequistos = await _processClickSign.AgreeBothRequisitos(envelopeId, Document, signatureOne, signatureTwo);


            if (AgreeRequistos.Contains("Erro"))
            {
                return BadRequest(new { error = AgreeRequistos });
            }


            var ActiveEnvelope = await _processClickSign.UpdateEnvelopeStatus(envelopeId);


            if (ActiveEnvelope.Contains("Erro"))
            {
                return BadRequest(new { error = ActiveEnvelope });
            }

            var signerNotification1 = await _processClickSign.CreateNotification(envelopeId, signatureOne);


            if (signerNotification1.Contains("Erro"))
            {
                return BadRequest(new { error = signerNotification1 });
            }


            var signerNotification2 = await _processClickSign.CreateNotification(envelopeId, signatureTwo);


            if (signerNotification2.Contains("Erro"))
            {
                return BadRequest(new { error = signerNotification2 });
            }

            var EnvelopeNotification = await _processClickSign.CreateEnvelopeNotification(envelopeId);

            if (EnvelopeNotification.Contains("Erro"))
            {
                return BadRequest(new { error = EnvelopeNotification });
            }





            return Ok(new { envelopeId });
        }









    }
}

const fs = require("fs");
const JSZip = require("jszip");

async function check(file) {
  const zip = await JSZip.loadAsync(fs.readFileSync(file));
  const names = Object.keys(zip.files);
  const slides = names.filter((n) => /^ppt\/slides\/slide\d+\.xml$/.test(n));
  const media = names.filter((n) => /^ppt\/media\//.test(n));
  const hasPresentation = names.includes("ppt/presentation.xml");
  let textCount = 0;
  for (const slide of slides) {
    const xml = await zip.file(slide).async("string");
    textCount += (xml.match(/<a:t>/g) || []).length;
  }
  console.log(`${file}: slides=${slides.length}, media=${media.length}, presentationXml=${hasPresentation}, textRuns=${textCount}`);
}

(async () => {
  await check("Prezentacja_1_Chmura_Azure_HairSalonBooking.pptx");
  await check("Prezentacja_2_Wzorce_Projektowe_HairSalonBooking.pptx");
})();

const STORAGE_KEY='bayi-rehberi-mobile-v1';
const $=id=>document.getElementById(id);
let dealers=[],selectedId=null,selectedCity='',editingId=null;

const clean=value=>String(value||'').trim();
const escapeHtml=value=>clean(value).replace(/[&<>'"]/g,ch=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[ch]));
const displayText=value=>clean(value).toLocaleLowerCase('tr-TR').replace(/(^|[\s/\-().,])([a-zçğıöşü])/g,(match,separator,letter)=>separator+letter.toLocaleUpperCase('tr-TR'));
const canonicalCity=value=>displayText(value);

async function load(){
  const saved=localStorage.getItem(STORAGE_KEY);
  if(saved){dealers=JSON.parse(saved);return}
  const source=await fetch('../SeedData.cs').then(r=>{if(!r.ok)throw new Error('Başlangıç bayi listesi alınamadı');return r.text()});
  const encoded=source.match(/Base64\s*=\s*"([^"]+)"/)?.[1];
  if(!encoded)throw new Error('Başlangıç bayi listesi çözülemedi');
  const text=new TextDecoder('utf-8').decode(Uint8Array.from(atob(encoded),character=>character.charCodeAt(0)));
  dealers=text.split(/\r?\n/).slice(1).filter(Boolean).map((line,index)=>{
    const [name='',city='',district='',phone='',address='']=line.split('\t');
    return{id:`seed-${index}`,name,city,district,phone,address};
  });
  save();
}
function save(){localStorage.setItem(STORAGE_KEY,JSON.stringify(dealers))}
function renderCities(){
  const cities=[...new Set(dealers.map(d=>canonicalCity(d.city)).filter(Boolean))].sort((a,b)=>a.localeCompare(b,'tr'));
  $('cityList').innerHTML=`<button class="city-option ${selectedCity?'':'active'}" type="button" data-city="">Tüm İller</button>`+cities.map(city=>`<button class="city-option ${city===selectedCity?'active':''}" type="button" data-city="${escapeHtml(city)}">${escapeHtml(displayText(city))}</button>`).join('');
  $('selectedCity').textContent=selectedCity?displayText(selectedCity):'İl Seçin';
}
function render(){
  const q=clean($('search').value).toLocaleLowerCase('tr-TR');
  const city=canonicalCity(selectedCity);
  const shown=dealers.filter(d=>(!city||canonicalCity(d.city)===city)&&clean(d.name).toLocaleLowerCase('tr-TR').includes(q)).sort((a,b)=>a.name.localeCompare(b.name,'tr'));
  $('count').textContent=shown.length;$('empty').hidden=shown.length>0;
  $('list').innerHTML=shown.map(d=>`<button class="dealer" data-id="${escapeHtml(d.id)}"><strong>${escapeHtml(displayText(d.name))}</strong><span>${escapeHtml([d.district,d.city].filter(Boolean).map(displayText).join(' / ')||'Konum Belirtilmemiş')}</span><span>${escapeHtml(d.phone||'Telefon Belirtilmemiş')}</span></button>`).join('');
}
function showDetail(id){
  const d=dealers.find(item=>item.id===id);if(!d)return;selectedId=id;$('detailName').textContent=displayText(d.name);
  $('detailFields').innerHTML=[['İl',displayText(d.city)],['İlçe',displayText(d.district)],['Telefon',d.phone],['Adres',displayText(d.address)]].map(([k,v])=>`<dt>${k}</dt><dd>${escapeHtml(v||'Belirtilmemiş')}</dd>`).join('');
  $('detailDialog').showModal();
}
function openForm(dealer=null){
  editingId=dealer?.id||null;
  $('addForm').reset();
  $('formEyebrow').textContent=editingId?'KAYDI DÜZENLE':'YENİ KAYIT';
  $('formTitle').textContent=editingId?'Bayi bilgilerini düzenle':'Yeni bayi ekle';
  $('saveButton').textContent=editingId?'Değişiklikleri kaydet':'Kaydet';
  if(dealer){for(const field of ['name','city','district','phone','address'])$('addForm').elements[field].value=dealer[field]||''}
  $('addDialog').showModal();
}
$('search').addEventListener('input',render);
$('cityPicker').addEventListener('click',()=>{$('cityDialog').showModal()});
$('cityList').addEventListener('click',e=>{const option=e.target.closest('[data-city]');if(!option)return;selectedCity=option.dataset.city;renderCities();render();$('cityDialog').close()});
$('list').addEventListener('click',e=>{const card=e.target.closest('[data-id]');if(card)showDetail(card.dataset.id)});
$('addButton').addEventListener('click',()=>openForm());
$('editButton').addEventListener('click',()=>{const dealer=dealers.find(item=>item.id===selectedId);if(!dealer)return;$('detailDialog').close();openForm(dealer)});
document.querySelectorAll('[data-close]').forEach(button=>button.addEventListener('click',()=>button.closest('dialog').close()));
$('addForm').addEventListener('submit',e=>{e.preventDefault();const data=new FormData(e.currentTarget);const values={name:clean(data.get('name')),city:canonicalCity(data.get('city')),district:displayText(data.get('district')),phone:clean(data.get('phone')),address:displayText(data.get('address'))};if(editingId){const index=dealers.findIndex(item=>item.id===editingId);if(index>=0)dealers[index]={...dealers[index],...values}}else dealers.push({id:`user-${Date.now()}-${crypto.randomUUID?.()||Math.random()}`,...values});save();renderCities();render();$('addDialog').close();editingId=null});
$('deleteButton').addEventListener('click',()=>{const d=dealers.find(item=>item.id===selectedId);if(!d||!confirm(`“${d.name}” kaydı silinsin mi?`))return;dealers=dealers.filter(item=>item.id!==selectedId);save();renderCities();render();$('detailDialog').close()});
load().then(()=>{renderCities();render()}).catch(error=>{$('empty').hidden=false;$('empty').textContent=error.message});
if('serviceWorker'in navigator)window.addEventListener('load',()=>navigator.serviceWorker.register('sw.js'));

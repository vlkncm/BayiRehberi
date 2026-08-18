const STORAGE_KEY='bayi-rehberi-mobile-v1';
const $=id=>document.getElementById(id);
let dealers=[],selectedId=null,selectedCity='';

const clean=value=>String(value||'').trim();
const escapeHtml=value=>clean(value).replace(/[&<>'"]/g,ch=>({'&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;'}[ch]));

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
  const cities=[...new Set(dealers.map(d=>clean(d.city)).filter(Boolean))].sort((a,b)=>a.localeCompare(b,'tr'));
  $('cityList').innerHTML=`<button class="city-option ${selectedCity?'':'active'}" type="button" data-city="">Tüm iller</button>`+cities.map(city=>`<button class="city-option ${city===selectedCity?'active':''}" type="button" data-city="${escapeHtml(city)}">${escapeHtml(city)}</button>`).join('');
  $('selectedCity').textContent=selectedCity||'İl seçin';
}
function render(){
  const q=clean($('search').value).toLocaleLowerCase('tr-TR');
  const city=selectedCity.toLocaleLowerCase('tr-TR');
  const shown=dealers.filter(d=>(!city||clean(d.city).toLocaleLowerCase('tr-TR')===city)&&clean(d.name).toLocaleLowerCase('tr-TR').includes(q)).sort((a,b)=>a.name.localeCompare(b.name,'tr'));
  $('count').textContent=shown.length;$('empty').hidden=shown.length>0;
  $('list').innerHTML=shown.map(d=>`<button class="dealer" data-id="${escapeHtml(d.id)}"><strong>${escapeHtml(d.name)}</strong><span>${escapeHtml([d.district,d.city].filter(Boolean).join(' / ')||'Konum belirtilmemiş')}</span><span>${escapeHtml(d.phone||'Telefon belirtilmemiş')}</span></button>`).join('');
}
function showDetail(id){
  const d=dealers.find(item=>item.id===id);if(!d)return;selectedId=id;$('detailName').textContent=d.name;
  $('detailFields').innerHTML=[['İl',d.city],['İlçe',d.district],['Telefon',d.phone],['Adres',d.address]].map(([k,v])=>`<dt>${k}</dt><dd>${escapeHtml(v||'Belirtilmemiş')}</dd>`).join('');
  $('detailDialog').showModal();
}
$('search').addEventListener('input',render);
$('cityPicker').addEventListener('click',()=>{$('cityDialog').showModal()});
$('cityList').addEventListener('click',e=>{const option=e.target.closest('[data-city]');if(!option)return;selectedCity=option.dataset.city;renderCities();render();$('cityDialog').close()});
$('list').addEventListener('click',e=>{const card=e.target.closest('[data-id]');if(card)showDetail(card.dataset.id)});
$('addButton').addEventListener('click',()=>{$('addForm').reset();$('addDialog').showModal()});
document.querySelectorAll('[data-close]').forEach(button=>button.addEventListener('click',()=>button.closest('dialog').close()));
$('addForm').addEventListener('submit',e=>{e.preventDefault();const data=new FormData(e.currentTarget);dealers.push({id:`user-${Date.now()}-${crypto.randomUUID?.()||Math.random()}`,name:clean(data.get('name')),city:clean(data.get('city')),district:clean(data.get('district')),phone:clean(data.get('phone')),address:clean(data.get('address'))});save();renderCities();render();$('addDialog').close()});
$('deleteButton').addEventListener('click',()=>{const d=dealers.find(item=>item.id===selectedId);if(!d||!confirm(`“${d.name}” kaydı silinsin mi?`))return;dealers=dealers.filter(item=>item.id!==selectedId);save();render();$('detailDialog').close()});
load().then(()=>{renderCities();render()}).catch(error=>{$('empty').hidden=false;$('empty').textContent=error.message});
if('serviceWorker'in navigator)window.addEventListener('load',()=>navigator.serviceWorker.register('sw.js'));

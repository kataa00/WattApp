import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
import { environment } from 'src/environments/environment';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Device } from '../../models/device.model';
import { Storage } from '../../models/storage.model';
import { newDeviceDTO } from 'src/app/models/newDeviceDTO';
import { CookieService } from 'ngx-cookie-service';
@Component({
  selector: 'app-add-device',
  templateUrl: './add-device.component.html',
  styleUrls: ['./add-device.component.css']
})
export class AddDeviceComponent {

  groups: any[] = [];
  selectedGroup!: string;

  manufacturers: any[] = [];
  selectedManufacturerId: any;
  devices: any[] = [];
  selectedDevice: any;
  selectedWattage: any;


  constructor(
    private fb: FormBuilder, 
    private router : Router,
    private http : HttpClient,
    private messageService: MessageService,
    private cookie: CookieService
  ){}

  addDeviceForm: FormGroup = this.fb.group({
    type:['', Validators.required],
    manufacturer: ['', Validators.required],
    device: ['', Validators.required],
    deviceName: ['', Validators.required],
    macAddress: ['', Validators.required]
  });

  ngOnInit() {
    this.http.get<any[]>(environment.apiUrl + '/api/Device/manufacturers')
      .subscribe(data => {
        this.manufacturers = data;
      });

    this.http.get<any[]>(environment.apiUrl + '/api/Device/groups')
      .subscribe(groups => this.groups = groups);
  }

  onGroupSelected(event: any) {
    const selectElement = event.target as HTMLSelectElement;
    this.selectedGroup = selectElement.value;
    console.log(this.selectedGroup);

    if (!this.selectedManufacturerId || !this.selectedGroup) {
      console.log('No manufacturer or group selected');
      return;
    }
    else{
      this.http.get<any[]>(`${environment.apiUrl}/api/Device/${this.selectedGroup}/${this.selectedManufacturerId}`)
      .subscribe({
        next: data => {
          console.log(data);
          this.devices = data;
        },
        error: err => {
          console.error(err);
        }
      });
    }
  }
  

  onManufacturerChange(event: any) {
    const manSelect = event.target as HTMLSelectElement;
    this.selectedManufacturerId = manSelect.value;
    console.log(this.selectedManufacturerId);
  
    if (!this.selectedManufacturerId || !this.selectedGroup) {
      console.log('No manufacturer or group selected');
      return;
    }
    else{
      this.http.get<any[]>(`${environment.apiUrl}/api/Device/${this.selectedGroup}/${this.selectedManufacturerId}`)
      .subscribe({
        next: data => {
          console.log(data);
          this.devices = data;
        },
        error: err => {
          console.error(err);
        }
      });
    } 
  }
  
  onDeviceChange(event: any){
    const deviceSelect = event.target as HTMLSelectElement;
    this.selectedDevice = deviceSelect.value;
    this.selectedWattage = this.devices.find(device => device.id === deviceSelect.value);
    console.log(this.selectedWattage);
  }

  onSubmit() {
      const formData = this.addDeviceForm.value;
      console.log(formData);
      const headers = new HttpHeaders()
        .set('Authorization', `Bearer ${this.cookie.get('jwtToken')}`); 
  
      this.http.post(environment.apiUrl + '/api/Device/devices/add-new', new newDeviceDTO(formData.macAddress, this.selectedDevice, formData.deviceName), { headers })
        .subscribe(response => {
          console.log(response);
          this.router.navigate(['home']);
        });
  }

  goBack(){
    this.router.navigate(['/home']);
  }

}
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { MessageService } from 'primeng/api';
//import { NgToastService } from 'ng-angular-popup';
@Component({
  selector: 'app-add-storage',
  templateUrl: './add-storage.component.html',
  styleUrls: ['./add-storage.component.css']
})
export class AddStorageComponent {
  submitted = false;
  addStorageForm!: FormGroup;
  toggle2Checked = false;

  constructor(
    private fb: FormBuilder, 
    private router : Router,
    //private toast : NgToastService
    private messageService: MessageService
  ){}

  onSubmit(){
    this.submitted = true;
    if(this.addStorageForm.valid){
      this.messageService.add({ severity: 'success', summary: 'Success', detail: 'You have added new device' });
      this.router.navigate(['home']);
      return;
    }else{
      this.messageService.add({ severity: 'error', summary: 'Error adding device', detail: 'Try again' });
      this.router.navigate(['addDevice'])
    }

  }
}

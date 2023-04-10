import { AfterViewInit, Component, ElementRef, OnInit, ViewChild } from '@angular/core';
import { deviceGroup, deviceGroupManifacturers, deviceManifacturers } from 'models/Devices';
import { AuthService } from 'service/auth.service';
import { Chart, elements } from 'chart.js';
import { ChartOptions } from 'chart.js';
import { User } from 'models/User';
import { animation } from '@angular/animations';
import { Root } from 'models/weather';
import { eachDevice } from 'models/eachDevice';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})
export class HomeComponent implements OnInit, AfterViewInit{
	
	
	eachDevicePrev!: eachDevice[];
	eachDeviceNext!: eachDevice[]
	

	constructor(
		private auth : AuthService
	){}
	

	totalUsers!: number;

	weather! : Root;
	User! : User[];
	
	previousMonthLabels: string[] = [];
	everyDayUsagePreviousMonth: any;
	everyDayUsageNextMonth:any;

	previousMonth! : any;
	nextMonth!: any;
	nextMonthLabels:string[] = [];
	nextMonthData: any;

	selectOption!: string;

	deviceGroup!: deviceGroup[];
	deviceManifaturers!: deviceManifacturers[];
	deviceManifacturersByGroupID!: deviceGroupManifacturers[];
	deviceGroupByGroupID!: deviceGroupManifacturers[]; 

	producers!: deviceGroupManifacturers[];
	consumers! : deviceGroupManifacturers[];
	storage!: deviceGroupManifacturers[]; 

	labProducers!: string[];
	labConsumers!: string[];
	labStorages!:string[];

	total!: number;
	data : any;
	
	chart!: any;
	chart1!: any;
	chart2!: any;
	chart3!: any;
	chart4!: any;
	
	@ViewChild('myChart') myChart!: ElementRef;
	@ViewChild('myChartUsers') myChartUsers!:ElementRef;
	@ViewChild('myChartForEveryTypeOfDevice') myChartForEveryTypeOfDevice!: ElementRef;
	@ViewChild('hourlyTemp') hourlyTemp!: ElementRef;
	@ViewChild('prevMonth') prevMonth!: ElementRef;
	@ViewChild('nextMonthChart') nextMonthChart! :ElementRef;
	@ViewChild('prevEachDevice') prevEachDevice!: ElementRef;
	@ViewChild('nextEachDevice') nextEachDevice!:ElementRef;
	
	ngAfterViewInit(): void {
		this.giveMeWeather();
		setTimeout(() =>{
			this.giveMeChartForTemperatureDaily();
		},0)
		
		this.getNumberOfUsers();
    	this.createMeChartForEveryDevice();
    	setTimeout(() => {
        	this.giveMeChartForUsers();
    	}, 0);
		
	}
							
							
	
	
		
	ngOnInit(): void {
		this.getDeviceGroup();
		this.powerUsagePreviousMonth();
		this.nextMonthSummary();
		this.previousMonthEachDevice();
		this.nextMonthEachDevice();
		this.giveMeWeather();
		
		this.getNumberOfUsers();
		this.createMeChartForEveryDevice();
		this.getAllUserInfo();
		this.giveMeChartForTemperatureDaily();
	}


	giveMeWeather(){
		this.auth.getWeather().subscribe(
			(response :any)=>{
				this.weather = response;
				this.giveMeChartForTemperatureDaily();
				
			}
		)
	}

	giveMeChartForTemperatureDaily(){
		const timeSlice = this.weather.hourly.time.slice(0,24);
		const time = timeSlice.map((time)=>{
			const date = new Date(time);
			const hours = date.getHours().toString().padStart(2,"0");
			const minutes = date.getMinutes().toString().padStart(2,"0");
			return hours+":"+minutes;
		})
		
		const labels = time;
		const data = {
		labels: labels,
		datasets: [{
			label: 'Temperature hourly',
			data: this.weather.hourly.temperature_2m,
			fill: true,
			borderColor: 'rgb(115, 210, 222)',
			backgroundColor:'rgb(115, 210, 222)',
			tension: 0.1
		}]
	}
	const options: ChartOptions = {
		scales: {
		  x: {
			title: {
			  display: true,
			  text: 'Temperature in celsius and x hourly',
			},
			ticks: {
			  font: {
				size: 14,
			  },
			},
		  },
		  y: {
			title: {
			  display: true,
			  text: 'Temperature (°C)',
			},
			ticks: {
			  font: {
				size: 14,
			  },
			},
		  },
		},
	  };
		const stackedLine = new Chart(this.hourlyTemp.nativeElement, {
			type: 'line',
			data: data,
			options: options,
		});
		
};
	
	
	getNumberOfUsers(){
		this.auth.getUserNumber().subscribe(
			(response : any) => {
				this.totalUsers = response;
			}
		)
	}

	getAllUserInfo(){
		this.auth.getAllUserInfo().subscribe(
			(response : any)=>{
				this.User = response;
			}
		)
	}
	
	getDeviceGroup(){
		this.auth.getDeviceGroup().subscribe(
			(response : any)=>{
				this.deviceGroup = response;
				for(let group of this.deviceGroup){
					 this.auth.getDeviceGroupID(group.id).subscribe(
						(response:any)=>{
							if(group.id === "77cbc929-1cf2-4750-900a-164de4abe28b")
							{
								this.producers = response;
								console.log('PRODUCERS',this.producers);
								
							}else if(group.id === "18f30035-59de-474f-b9db-987476de551f")
							{
								this.consumers = response;
							}
							else if(group.id === "b17c9155-7e6f-4d37-8a86-ea1abb327bb2")
							{
								this.storage = response;
							}
							
							this.total = this.producers.length + this.consumers.length + this.storage.length;
							this.createChartDevices();
							
							 
							this.labProducers = [...new Set(this.producers.map(element => element.name))];
							this.labConsumers = [...new Set(this.consumers.map(element => element.name))];
							this.labStorages = [...new Set(this.storage.map(element => element.name))];
							
							this.createMeChartForEveryDevice();
							this.getNumberOfUsers();
							this.giveMeChartForUsers();
						}
					 )
					}
				}
			)	
		}
		
		  
	createChartDevices(){
		this.chart = new Chart(this.myChart.nativeElement, {
		type: 'doughnut',
		data: {
			labels: ['Prosumer', 'Consumer', 'Storage'],
			datasets: [{
			data: [this.producers.length, this.consumers.length, this.storage.length],
			backgroundColor: [
				'rgba(255, 159, 64, 0.5)',
				'rgba(54, 162, 235, 0.5)',
				'rgba(75, 192, 192, 0.5)'
				
			],
			borderColor:[
				'rgb(255, 159, 64)',
				'rgb(54, 162, 235)',
				'rgb(75, 192, 192)',
			],
			hoverOffset: 4,
			borderWidth: 1,
			}]
		},
		options: {
			responsive: true,
			maintainAspectRatio: false
		}
		});
	}

	

	giveMeChartForUsers(){
		const tot = this.totalUsers;
		this.chart1 = new Chart(this.myChartUsers.nativeElement, {
			type: 'doughnut',
			data: {
				labels: ['Users'],
				datasets: [{
				data: [this.totalUsers, 100-this.totalUsers],
				backgroundColor: [
					'rgb(241, 143, 1)',
					'rgb(255, 255, 255)'
				],
				borderWidth: 0
				}]
			},
			options: {
				cutout:'60',
				aspectRatio:30,
				responsive: true,
      			maintainAspectRatio: false,
			}
		});
	}

	

	createMeChartForEveryDevice(){
		const dataProducers: number[] = [];
		let label : string[] = this.labProducers.concat(this.labConsumers).concat(this.labStorages);
		
		this.producers.forEach(element => {
		const index = label.indexOf(element.name);
			dataProducers[index] = dataProducers[index] ? dataProducers[index] + 1 : 1;
		});

		const dataConsumers: number[] = [];
		
		this.consumers.forEach(element => {
		const index = label.indexOf(element.name);
			dataConsumers[index] = dataConsumers[index] ? dataConsumers[index] + 1 : 1;
		});

		const dataStorages: number[] = [];
		
		this.storage.forEach(element => {
		const index = label.indexOf(element.name);
			dataStorages[index] = dataStorages[index] ? dataStorages[index] + 1 : 1;
		});

		
	
	

	const chartData = {
    labels: label,
    datasets: [
        {
            label: 'Producers',
            backgroundColor: 'rgba(255, 159, 64, 0.5)',
            borderColor: 'rgb(255, 159, 64)',
            borderWidth: 1,
            data: dataProducers
        },
        {
            label: 'Consumers',
            backgroundColor: 'rgba(54, 162, 235, 0.5)',
            borderColor:   'rgb(54, 162, 235)',
            borderWidth: 1,
            data: dataConsumers
        },
        {
            label: 'Storages',
            backgroundColor: 'rgba(75, 192, 192, 0.5)',
            borderColor: 'rgb(75, 192, 192)',
            borderWidth: 1,
            data: dataStorages
        }
    	]
	};

		this.chart = new Chart(this.myChartForEveryTypeOfDevice.nativeElement, {
			type: 'bar',
			data: chartData,
			options: {
				indexAxis: 'y',
				scales: {
					y: {
						beginAtZero: true
					}
				}
			}
		});
		}

	
	
		powerUsagePreviousMonth(){
			this.auth.getPowerUsagePreviousMonthSummary().subscribe(
				(response : any) => {
					this.previousMonth = response;
					this.previousMonthEveryDay()
				}
			)
		}
	
		previousMonthEveryDay(){
			this.auth.getPowerUsagePreviousMonthEveryDayUsage().subscribe(
				(response:any)=>{
					this.everyDayUsagePreviousMonth = response;
					this.charthForPreviusMonth();
					
				}
			)
		}
	
		
	
		charthForPreviusMonth(){
				 
				 const list =  Object.keys(this.everyDayUsagePreviousMonth).map((key) => key.split('T')[0]);
				 
				 const valuesList = [];
	
				for (const key in this.everyDayUsagePreviousMonth) {
					if (this.everyDayUsagePreviousMonth.hasOwnProperty(key)) {
						valuesList.push(this.everyDayUsagePreviousMonth[key]);
					}
				}
				
	
				
				const data = {
				labels: list,
				datasets: [{
					label: 'Power Usage For Previous Month',
					data: valuesList,
					fill: true,
					borderColor: 'rgb(255, 159, 64)',
					backgroundColor:'rgba(255, 159, 64, 0.5)',
					tension: 0.1
				}]
			}
				const options: ChartOptions = {
					scales: {
					x: {
						title: {
						display: true,
						text: 'Date and Time',
						},
						ticks: {
						font: {
							size: 14,
						},
						},
					},
					y: {
						title: {
						display: true,
						text: 'Power Consuming in (kw/day)',
						},
						ticks: {
						font: {
							size: 14,
						},
						},
					},
					},
				};
				const stackedLine = new Chart(this.prevMonth.nativeElement, {
					type: 'line',
					data: data,
					options: options,
				});
		}
	
		nextMonthSummary(){
			this.auth.getPowerUsageNextMonthSummary().subscribe(
				(response : any)=>{
					
					this.nextMonthSummary = response;
					this.nextMonthEveryDay();
				}
			)
		}
	
		nextMonthEveryDay(){
			this.auth.getPowerUsageNextMonthEveryDay().subscribe(
				(response :any)=>{
					
					this.everyDayUsageNextMonth = response;
					this.chartForNextMonth();
				}
			)
		}
		chartForNextMonth(){
			const list =  Object.keys(this.everyDayUsageNextMonth).map((key) => key.split('T')[0]);
			const valuesList = [];
	
			for (const key in this.everyDayUsageNextMonth) {
				if (this.everyDayUsageNextMonth.hasOwnProperty(key)) {
					valuesList.push(this.everyDayUsageNextMonth[key]);
				}
			}
	
		   const data = {
		   labels: list,
		   datasets: [{
			   label: 'Power Usage For Next Month',
			   data: valuesList,
			   fill: true,
			   borderColor: 'rgb(75, 192, 192)',
			   backgroundColor:'rgba(75, 192, 192, 0.5)',
			   tension: 0.1,
			   borderWidth: 1,
		   }]
	   }
		   const options: ChartOptions = {
			   scales: {
			   x: {
				   title: {
				   display: true,
				   text: 'Date and Time',
				   },
				   ticks: {
				   font: {
					   size: 14,
				   },
				   },
			   },
			   y: {
				   title: {
				   display: true,
				   text: 'Power Consuming in (kw/day)',
				   },
				   ticks: {
				   font: {
					   size: 14,
				   },
				   },
			   },
			   },
		   };
		   const stackedLine = new Chart(this.nextMonthChart.nativeElement, {
			   type: 'line',
			   data: data,
			   options: options,
		   });
		}
	
	
		previousMonthEachDevice(){
			this.auth.getPowerUsagePreviousMonthEachDevice().subscribe(
				(response : any) =>{
					this.eachDevicePrev = response;
					this.chartPreviousMonthEachDevice()
					
				}
			)
		}
	
		chartPreviousMonthEachDevice(){
			const label: string[] =[];
			const producersUnique = Array.from(new Set(this.producers))
			for(const key in this.eachDevicePrev){
				for(let pro of producersUnique){
					if(key === pro.id){
						label.push(pro.name);
					}
				}
			}
			console.log("LABEL",label)
			const dataEach = Object.values(this.eachDevicePrev).map((value) => value);
			const data = {
				labels: label,
				datasets: [{
					label: 'Previous Month for Each Device',
					data: dataEach,
					fill: true,
					borderColor: 'rgb(255, 159, 64)',
					backgroundColor:'rgba(255, 159, 64, 0.5)',
					tension: 0.1,
					borderWidth: 1,
				}]
			}
				const options: ChartOptions = {
					indexAxis: 'y',
					scales: {
					x: {
						title: {
						display: true,
						text: 'Power Consuming in (kw/day)',
						},
						ticks: {
						font: {
							size: 14,
						},
						},
					},
					y: {
						title: {
						display: true,
						text: '',
						},
						ticks: {
						font: {
							size: 14,
						},
						},
					},
					},
				};
				const stackedLine = new Chart(this.prevEachDevice.nativeElement, {
					type: 'bar',
					data: data,
					options: options,
				});
		}
	
		nextMonthEachDevice(){
			this.auth.getPowerUsageNextMonthEachDevice().subscribe(
				(response : any)=>{
					this.eachDeviceNext = response;
					this.chartNextMonthEachDevice();
				}
			)
		}
	
		chartNextMonthEachDevice(){
			const label: string[] =[];
			const producersUnique = Array.from(new Set(this.producers))
			
			for(const key in this.eachDeviceNext){
				for(let pro of producersUnique){
					if(key === pro.id){
						label.push(pro.name);
					}
				}
			}
	
			const dataEach = Object.values(this.eachDeviceNext).map((value) => value);
			const data = {
				labels: label,
				datasets: [{
					label: 'Next Month for Each Device',
					data: dataEach,
					fill: true,
					borderColor: 'rgb(75, 192, 192)',
					   backgroundColor:'rgba(75, 192, 192, 0.5)',
					tension: 0.1,
					borderWidth: 1,
				}]
			}
				const options: ChartOptions = {
					indexAxis: 'y',
					scales: {
					x: {
						title: {
						display: true,
						text: 'Power Consuming in (kw/day)',
						},
						ticks: {
						font: {
							size: 14,
						},
						},
					},
					y: {
						title: {
						display: true,
						text: '',
						},
						ticks: {
						font: {
							size: 14,
						},
						},
					},
					},
				};
				const stackedLine = new Chart(this.nextEachDevice.nativeElement, {
					type: 'bar',
					data: data,
					options: options,
				});
		}
	
}

               


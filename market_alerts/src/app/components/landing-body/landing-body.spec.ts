import { ComponentFixture, TestBed } from '@angular/core/testing';

import { LandingBody } from './landing-body';

describe('LandingBody', () => {
  let component: LandingBody;
  let fixture: ComponentFixture<LandingBody>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LandingBody]
    })
    .compileComponents();

    fixture = TestBed.createComponent(LandingBody);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});

import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  beforeEach,
  describe,
  expect,
  it,
} from 'vitest';

import { ConfirmDialog } from './confirm-dialog';

describe('ConfirmDialog', () => {
  let component: ConfirmDialog;
  let fixture: ComponentFixture<ConfirmDialog>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmDialog],
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmDialog);
    component = fixture.componentInstance;
  });

  it('should create the component', () => {
    expect(component).toBeTruthy();
  });

  it('should expose the default input values', () => {
    expect(component.isOpen()).toBe(false);
    expect(component.title()).toBe('Confirm action');
    expect(component.message()).toBe('');
    expect(component.confirmLabel()).toBe('Confirm');
    expect(component.cancelLabel()).toBe('Cancel');
  });

  it('should accept custom input values', () => {
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput(
      'title',
      'Delete task',
    );
    fixture.componentRef.setInput(
      'message',
      'This task will be deleted.',
    );
    fixture.componentRef.setInput(
      'confirmLabel',
      'Delete',
    );
    fixture.componentRef.setInput(
      'cancelLabel',
      'Keep task',
    );

    fixture.detectChanges();

    expect(component.isOpen()).toBe(true);
    expect(component.title()).toBe('Delete task');
    expect(component.message()).toBe(
      'This task will be deleted.',
    );
    expect(component.confirmLabel()).toBe('Delete');
    expect(component.cancelLabel()).toBe('Keep task');
  });

  it('should emit cancelled when escape is pressed while open', () => {
    let cancelled = false;

    component.cancelled.subscribe(() => {
      cancelled = true;
    });

    fixture.componentRef.setInput('isOpen', true);
    fixture.detectChanges();

    component.closeWithEscape();

    expect(cancelled).toBe(true);
  });

  it('should not emit cancelled when escape is pressed while closed', () => {
    let cancelled = false;

    component.cancelled.subscribe(() => {
      cancelled = true;
    });

    fixture.componentRef.setInput('isOpen', false);
    fixture.detectChanges();

    component.closeWithEscape();

    expect(cancelled).toBe(false);
  });
});